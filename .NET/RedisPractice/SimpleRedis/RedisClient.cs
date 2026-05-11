using SimpleRedis.Enum;
using SimpleRedis.Exceptions;
using SimpleRedis.Helper;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace SimpleRedis
{
    /// <summary>
    /// 自定义的Redis客户端类，用于与Redis服务器进行通信
    /// </summary>
    public class RedisClient : IDisposable
    {
        private string _host;
        private int _port;
        private string _username;
        private string _password;
        private int _db;
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private int _connectStatus;
        private static ConcurrentDictionary<string, Lazy<Task<RedisClient>>> redisClients = new();

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            redisClients.TryRemove(GetClientKey(_host, _port, _username), out _);
            _stream?.Dispose();
            _tcpClient?.Dispose();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <param name="stream"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="dbNum"></param>
        public RedisClient(TcpClient tcpClient, NetworkStream stream, string host, int port, string userName, string password, int dbNum = 0)
        {
            _host = host;
            _port = port;
            _username = userName;
            _password = password;
            _db = dbNum;
            _tcpClient = tcpClient;
            _stream = stream;
            _connectStatus = (int)TcpConnectStatusEnum.Ununited;
        }

        /// <summary>
        /// 异步工厂方法创建RedisClient实例
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="dbNum"></param>
        /// <returns></returns>
        public static Task<RedisClient> CreateClientAsync(string host, int port, string userName, string password, int dbNum = 0)
        {
            var clientKey = GetClientKey(host, port, userName);
            try
            {
                var lazyClient = redisClients.GetOrAdd(clientKey, _ => new Lazy<Task<RedisClient>>(() => CreateNewClientAsync(host, port, userName, password, dbNum)));
                return lazyClient.Value;
            }
            catch (SocketException ex)
            {
                redisClients.TryRemove(clientKey, out _);
                throw;
            }
        }

        /// <summary>
        /// 创建客户端实例并连接到Redis服务器
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="dbNum"></param>
        /// <returns></returns>
        public static async Task<RedisClient> GetClientAndConnectAsync(string host, int port, string userName, string password, int dbNum = 0)
        {
            var client = await CreateClientAsync(host, port, userName, password, dbNum);
            await client.ConnectAsync();
            return client;
        }

        /// <summary>
        /// 具体的创建RedisClient实例的逻辑，包括连接到Redis服务器和进行身份验证  
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="dbNum"></param>
        /// <returns></returns>
        /// <exception cref="RedisConnectionException"></exception>
        private static async Task<RedisClient> CreateNewClientAsync(string host, int port, string userName, string password, int dbNum = 0)
        {
            try
            {
                var tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(host, port);
                var stream = tcpClient.GetStream();

                var newClient = new RedisClient(tcpClient, stream, host, port, userName, password, dbNum);
                return newClient;
            }
            catch (SocketException ex)
            {
                throw new RedisConnectionException($"无法连接到Redis服务器 {host}:{port}", ex);
            }
        }

        /// <summary>
        /// 计算RedisClient在字典中的键，使用主机、端口和用户名的组合作为唯一标识
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        private static string GetClientKey(string host, int port, string userName)
        {
            return $"{host}:{port}:{userName}";
        }

        /// <summary>
        /// 连接到Redis服务器并进行身份验证
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task ConnectAsync()
        {
            if (Interlocked.CompareExchange(ref _connectStatus, (int)TcpConnectStatusEnum.Connecting, (int)TcpConnectStatusEnum.Ununited) == (int)TcpConnectStatusEnum.Ununited)
            {

                if (!string.IsNullOrWhiteSpace(_username) || !string.IsNullOrWhiteSpace(_password))
                {
                    var authCommand = string.Empty;
                    if (string.IsNullOrWhiteSpace(_username))
                    {
                        authCommand = $"AUTH {_password}";
                    }
                    else
                    {
                        authCommand = $"AUTH {_username} {_password}";
                    }
                    var result = await SendCommandAsync(TransitionCommand(authCommand));
                    if (string.Equals(result, "ok", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _ = Interlocked.CompareExchange(ref _connectStatus, (int)TcpConnectStatusEnum.Connected, (int)TcpConnectStatusEnum.Connecting);
                    }
                    else
                    {
                        throw new RedisAuthenticationException("用户名或者密码错误");
                    }
                }
            }
        }

        public async Task CloseConnectAsync()
        {
            if (_tcpClient.Connected)
            {
                _tcpClient.Close();
            }
        }

        /// <summary>
        /// 发送命令
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<string> SendCommandAsync(string command)
        {
            try
            {
                var sendBuffer = Encoding.UTF8.GetBytes(command);
                await _stream.WriteAsync(sendBuffer, 0, sendBuffer.Length);

                using var memoryStream = new MemoryStream();
                var receiveBuffer = new byte[1024];
                var readLength = 0;
                var socketResult = await NetworkHelper.StockPollWaitForStreamToBeReadable(_tcpClient, 5000);
                if (!socketResult)
                {
                    throw new Exception("Socket Poll Timeout");
                }
                while (_stream.DataAvailable && (readLength = await _stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length)) > 0)
                {
                    memoryStream.Write(receiveBuffer, 0, readLength);
                }
                var byteArray = memoryStream.ToArray();
                return (string)AnalysisRequest(byteArray);


                //await NetworkHelper.SimpleWaitForStreamToBeReadableAsync(_stream);
                //using MemoryStream memoryStream = new MemoryStream();
                //await _stream.CopyToAsync(memoryStream);
                //var byteArray = memoryStream.ToArray();
                //return AnalysisRequest(byteArray);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 将输入的命令转换为redis协议
        /// </summary>
        /// <param name="command">输入的命令</param>
        /// <returns>转换为符合Redis协议的命令</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public string TransitionCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                throw new ArgumentNullException("command", "参数为空");
            }

            int orderCount = 0;
            var sbCommand = new StringBuilder();
            var commandArray = command.Split(' ');
            foreach (var item in commandArray)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    continue;
                }
                orderCount++;
                sbCommand.Append($"${item.Length}\r\n{item}\r\n");
            }
            sbCommand.Insert(0, $"*{orderCount}\r\n");
            return sbCommand.ToString();
        }

        /// <summary>
        /// 转换Redis服务端响应的数据
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public object AnalysisRequest(byte[] bytes)
        {
            var firstChar = bytes[0];
            switch (firstChar)
            {
                case (byte)'+'://响应数据为简单字符串
                case (byte)'-'://响应数据为错误信息
                case (byte)':'://响应数据为整数
                    return AnalysisSimpleOrErrorString(bytes);
                case (byte)'$'://响应数据为批量字符串
                    return AnalysisBatchString(bytes);
                case (byte)'*'://响应数据为数组
                    break;
                case (byte)'%'://响应数据为Map(哈希表)
                    break;
                case (byte)'~'://响应数据为Set(集合)
                    break;
                case (byte)'#'://响应数据为布尔值
                    break;
                case (byte)'_'://Null
                    break;
                case (byte)','://响应数据为浮点数
                    break;
                case (byte)'>'://响应数据为Push消息
                    break;
                default:
                    break;
            }
            return null;
        }

        /// <summary>
        /// 解析简单字符串或者错误信息
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private string AnalysisSimpleOrErrorString(byte[] bytes)
        {
            var result = Encoding.UTF8.GetString(bytes[1..^2]);
            return result;
        }

        /// <summary>
        /// 解析批量字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private string AnalysisBatchString(byte[] bytes)
        {
            int lengthStartIndex = 1;
            int lengthEndIndex = 1;
            for (int i = 1; i < bytes.Length; i++)
            {
                if (bytes[i + 1] == '\r' && bytes[i + 2] == '\n')
                {
                    break;
                }
                lengthEndIndex++;
            }

            var stringLength = Convert.ToInt32(Encoding.UTF8.GetString(bytes.AsSpan(lengthStartIndex, lengthEndIndex - lengthStartIndex + 1)));
            if (stringLength <= 0)
            {
                return string.Empty;
            }
            return Encoding.UTF8.GetString(bytes.AsSpan(lengthEndIndex + 3, stringLength));
        }
    }
}
