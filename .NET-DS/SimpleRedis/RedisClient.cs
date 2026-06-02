using SimpleRedis.Enum;
using SimpleRedis.Exceptions;
using System.Collections.Concurrent;
using System.Text;

namespace SimpleRedis;

/// <summary>
/// Redis 客户端入口，管理连接池和连接生命周期
/// </summary>
public class RedisClient : IRedisClient, IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly int _defaultDb;
    private int _db;
    private int _authenticateStatus; // 0=Unauthenticated,1=Authenticating,2=Authenticated,3=AuthenticationFailed
    private bool _isDisposed;

    private RedisConnection _connection;

    // 连接池: key = host:port:username
    private static readonly ConcurrentDictionary<string, Lazy<RedisClient>> _connectionPool = new();

    /// <summary>当前数据库编号</summary>
    public int Db
    {
        get => _db;
        set
        {
            if (value < 0 || value > 15)
                throw new ArgumentOutOfRangeException(nameof(value), "数据库编号必须在 0-15 之间");
            _db = value;
        }
    }

    private RedisClient(string host, int port, string username, string password, int dbNum)
    {
        _host = host;
        _port = port;
        _username = username;
        _password = password;
        _defaultDb = dbNum;
        _db = dbNum;
        _connection = new RedisConnection(host, port);
        _connection.OnDisconnected += HandleDisconnectedAsync;
    }

    /// <summary>
    /// 工厂方法：从连接池获取或创建 RedisClient 实例
    /// </summary>
    public static RedisClient CreateClient(string host, int port, string username = "", string password = "", int dbNum = 0)
    {
        var key = $"{host}:{port}:{username}";
        return _connectionPool.GetOrAdd(key, _ =>
            new Lazy<RedisClient>(() => new RedisClient(host, port, username, password, dbNum))
        ).Value;
    }

    /// <summary>
    /// 建立连接并完成认证
    /// </summary>
    public async Task ConnectAsync()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        await _connection.ConnectAsync();

        if (!string.IsNullOrWhiteSpace(_username) || !string.IsNullOrWhiteSpace(_password))
        {
            await AuthenticateAsync();
        }

        // 切换到默认数据库
        if (_defaultDb != 0)
        {
            var selectCmd = RESPEncoder.EncodeArgs("SELECT", _defaultDb.ToString());
            await SendCommandAsync(selectCmd);
        }
    }

    /// <summary>
    /// 连接池中已有的客户端数量
    /// </summary>
    public static int PoolCount => _connectionPool.Count;

    /// <summary>
    /// 发送 RESP 命令并返回解码后的结果
    /// </summary>
    public async Task<RedisResult> SendCommandAsync(string respCommand)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (!_connection.IsConnected)
            await _connection.ReconnectAsync();

        var data = Encoding.UTF8.GetBytes(respCommand);
        await _connection.SendAsync(data);
        var response = await _connection.ReceiveAsync();
        return RESPDecoder.Decode(response);
    }

    /// <summary>
    /// 使用 CAS 确保仅一个线程执行认证
    /// </summary>
    internal async Task AuthenticateAsync()
    {
        var status = Volatile.Read(ref _authenticateStatus);
        while (status is (int)AuthenticateStatusEnum.Unauthenticated or (int)AuthenticateStatusEnum.AuthenticationFailed)
        {
            if (Interlocked.CompareExchange(ref _authenticateStatus, (int)AuthenticateStatusEnum.Authenticating, status) == status)
            {
                try
                {
                    var authResp = string.IsNullOrWhiteSpace(_username)
                        ? await SendCommandAsync(RESPEncoder.EncodeArgs("AUTH", _password))
                        : await SendCommandAsync(RESPEncoder.EncodeArgs("AUTH", _username, _password));

                    var result = authResp.AsString();
                    if (string.Equals(result, "OK", StringComparison.OrdinalIgnoreCase))
                    {
                        Volatile.Write(ref _authenticateStatus, (int)AuthenticateStatusEnum.Authenticated);
                    }
                    else
                    {
                        Volatile.Write(ref _authenticateStatus, (int)AuthenticateStatusEnum.AuthenticationFailed);
                        throw new RedisAuthenticationException("Redis 认证失败");
                    }
                    return;
                }
                catch (RedisAuthenticationException) { throw; }
                catch (Exception ex)
                {
                    Volatile.Write(ref _authenticateStatus, (int)AuthenticateStatusEnum.AuthenticationFailed);
                    throw new RedisAuthenticationException("Redis 认证过程中发生异常", ex);
                }
            }
            await Task.Delay(50);
            status = Volatile.Read(ref _authenticateStatus);
        }

        if (status == (int)AuthenticateStatusEnum.AuthenticationFailed)
            throw new RedisAuthenticationException("Redis 认证失败");
    }

    /// <summary>
    /// 断开连接并从连接池中移除
    /// </summary>
    public async Task CloseAsync()
    {
        // 从连接池移除
        var key = $"{_host}:{_port}:{_username}";
        _connectionPool.TryRemove(key, out _);

        await _connection.DisconnectAsync();
        _isDisposed = true;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        var key = $"{_host}:{_port}:{_username}";
        _connectionPool.TryRemove(key, out _);

        _connection.OnDisconnected -= HandleDisconnectedAsync;
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    // 连接断开时的处理：将自身从连接池移除
    private Task HandleDisconnectedAsync()
    {
        var key = $"{_host}:{_port}:{_username}";
        _connectionPool.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
