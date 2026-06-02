using SimpleRedis.Enum;
using SimpleRedis.Exceptions;
using SimpleRedis.Helper;
using System.Net.Sockets;
using System.Text;

namespace SimpleRedis;

/// <summary>
/// Redis TCP 连接管理，负责连接的建立、关闭、心跳保活和断线重连
/// </summary>
internal class RedisConnection : IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private readonly int _connectionTimeout;
    private readonly int _receiveTimeout;

    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private int _connectStatus; // 使用 CAS 控制: 0=Disconnected,1=Connecting,2=Connected,3=ConnectionFailed
    private Timer? _heartbeatTimer;
    private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(30);
    private DateTime _lastActivityTime;
    private readonly object _lock = new();
    private bool _isDisposed;
    private bool _heartbeatEnabled;

    /// <summary>连接断开时触发，用于通知上层自动重连</summary>
    public event Func<Task>? OnDisconnected;

    /// <summary>当前连接状态</summary>
    public bool IsConnected => Volatile.Read(ref _connectStatus) == (int)TcpConnectStatusEnum.Connected;

    public RedisConnection(string host, int port, int connectionTimeout = 5000, int receiveTimeout = 5000)
    {
        _host = host;
        _port = port;
        _connectionTimeout = connectionTimeout;
        _receiveTimeout = receiveTimeout;
        _lastActivityTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 建立 TCP 连接，使用 CAS 防止并发连接
    /// </summary>
    public async Task ConnectAsync()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var status = Volatile.Read(ref _connectStatus);
        while (status == (int)TcpConnectStatusEnum.Disconnected ||
               status == (int)TcpConnectStatusEnum.ConnectionFailed)
        {
            if (Interlocked.CompareExchange(ref _connectStatus, (int)TcpConnectStatusEnum.Connecting, status) == status)
            {
                try
                {
                    _tcpClient = new TcpClient();
                    using var cts = new CancellationTokenSource(_connectionTimeout);
                    await _tcpClient.ConnectAsync(_host, _port, cts.Token);
                    _stream = _tcpClient.GetStream();
                    _lastActivityTime = DateTime.UtcNow;

                    Volatile.Write(ref _connectStatus, (int)TcpConnectStatusEnum.Connected);
                    StartHeartbeat();
                    return;
                }
                catch (OperationCanceledException)
                {
                    Volatile.Write(ref _connectStatus, (int)TcpConnectStatusEnum.ConnectionFailed);
                    _tcpClient?.Dispose();
                    _tcpClient = null;
                    throw new RedisTimeoutException($"连接 Redis 服务器 {_host}:{_port} 超时");
                }
                catch (Exception ex)
                {
                    Volatile.Write(ref _connectStatus, (int)TcpConnectStatusEnum.ConnectionFailed);
                    _tcpClient?.Dispose();
                    _tcpClient = null;
                    throw new RedisConnectionException($"无法连接到 Redis 服务器 {_host}:{_port}", ex);
                }
            }
            // 等待其他线程完成连接
            await Task.Delay(50);
            status = Volatile.Read(ref _connectStatus);
        }

        if (status == (int)TcpConnectStatusEnum.ConnectionFailed)
            throw new RedisConnectionException($"无法连接到 Redis 服务器 {_host}:{_port}");
    }

    /// <summary>
    /// 发送原始数据到 TCP 流
    /// </summary>
    public async Task SendAsync(byte[] data)
    {
        EnsureConnected();

        try
        {
            await _stream!.WriteAsync(data);
            await _stream.FlushAsync();
            _lastActivityTime = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            await HandleConnectionErrorAsync();
            throw new RedisIOException("发送数据失败", ex);
        }
    }

    /// <summary>
    /// 从 TCP 流接收响应数据
    /// </summary>
    public async Task<byte[]> ReceiveAsync()
    {
        EnsureConnected();

        var pollResult = await NetworkHelper.PollForDataAsync(_tcpClient!, _receiveTimeout);
        if (!pollResult)
        {
            await HandleConnectionErrorAsync();
            throw new RedisTimeoutException("等待服务器响应超时");
        }

        try
        {
            using var buffer = new MemoryStream();
            var readBuffer = new byte[8192];
            int bytesRead;

            while (_stream!.DataAvailable &&
                   (bytesRead = await _stream.ReadAsync(readBuffer, 0, readBuffer.Length)) > 0)
            {
                buffer.Write(readBuffer, 0, bytesRead);
            }

            if (buffer.Length == 0)
            {
                await HandleConnectionErrorAsync();
                throw new RedisConnectionException("连接已关闭");
            }

            _lastActivityTime = DateTime.UtcNow;
            return buffer.ToArray();
        }
        catch (RedisConnectionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await HandleConnectionErrorAsync();
            throw new RedisIOException("接收数据失败", ex);
        }
    }

    /// <summary>
    /// 主动关闭连接
    /// </summary>
    public async Task DisconnectAsync()
    {
        StopHeartbeat();
        try
        {
            if (_stream != null)
            {
                // 尝试发送 QUIT
                var quitCmd = Encoding.UTF8.GetBytes("*1\r\n$4\r\nQUIT\r\n");
                await _stream.WriteAsync(quitCmd);
            }
        }
        catch { /* 忽略关闭时的错误 */ }
        finally
        {
            Cleanup();
        }
    }

    /// <summary>
    /// 断线重连
    /// </summary>
    public async Task ReconnectAsync()
    {
        Cleanup();
        Volatile.Write(ref _connectStatus, (int)TcpConnectStatusEnum.Disconnected);
        await ConnectAsync();
    }

    private void StartHeartbeat()
    {
        _heartbeatEnabled = true;
        _heartbeatTimer = new Timer(async _ =>
        {
            if (!_heartbeatEnabled) return;

            try
            {
                if (IsConnected && (DateTime.UtcNow - _lastActivityTime) > _heartbeatInterval)
                {
                    var pingResp = Encoding.UTF8.GetBytes("*1\r\n$4\r\nPING\r\n");
                    await SendAsync(pingResp);
                    await ReceiveAsync();
                }
            }
            catch
            {
                // 心跳失败，触发断线处理
                await HandleConnectionErrorAsync();
            }
        }, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }

    private void StopHeartbeat()
    {
        _heartbeatEnabled = false;
        _heartbeatTimer?.Dispose();
        _heartbeatTimer = null;
    }

    private async Task HandleConnectionErrorAsync()
    {
        var wasConnected = Interlocked.Exchange(ref _connectStatus, (int)TcpConnectStatusEnum.ConnectionFailed);
        Cleanup();

        if (wasConnected == (int)TcpConnectStatusEnum.Connected && OnDisconnected != null)
        {
            await OnDisconnected.Invoke();
        }
    }

    private void Cleanup()
    {
        StopHeartbeat();
        _stream?.Dispose();
        _stream = null;
        _tcpClient?.Dispose();
        _tcpClient = null;
    }

    private void EnsureConnected()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (!IsConnected || _tcpClient == null || _stream == null)
            throw new RedisConnectionException("Redis 连接未建立或已断开");
    }

    public void Dispose()
    {
        _isDisposed = true;
        Cleanup();
        GC.SuppressFinalize(this);
    }
}
