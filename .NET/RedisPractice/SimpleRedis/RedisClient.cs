using SimpleRedis.Helper;
using System.Buffers.Binary;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;

namespace SimpleRedis
{
    public class RedisClient : IDisposable
    {
        private string _host;
        private int _port;
        private string _username;
        private string _password;
        private int _db;
        private TcpClient _tcpClient;
        private NetworkStream _stream;

        public void Dispose()
        {
            _stream?.Dispose();
            _tcpClient?.Dispose();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="dbNum"></param>
        public RedisClient(string host, int port, string userName, string password, int dbNum = 0)
        {
            _host = host;
            _port = port;
            _username = userName;
            _password = password;
            _db = dbNum;
        }

        public async Task ConnectAsync()
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(_host, _port);
            _stream = _tcpClient.GetStream();

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
                if (!string.Equals(result, "ok", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new Exception("用户名或者密码错误");
                }
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
                return AnalysisRequest(byteArray);


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
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<string> PingAsync()
        {
            return await SendCommandAsync("*1\r\n$4\r\nPING\r\n");
        }

        public async Task<string> SetAsync(string key, string value)
        {
            var command = $"set {key} {value}";
            return await SendCommandAsync(TransitionCommand(command));
        }

        public async Task<string> GetAsync(string key)
        {
            var command = $"get {key}";
            return await SendCommandAsync(TransitionCommand(command));
        }

        public async Task<long> IncrAsync(string key)
        {
            var command = $"incr {key}";
            return Convert.ToInt64(await SendCommandAsync(TransitionCommand(command)));
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

        public string AnalysisRequest(byte[] bytes)
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
            return string.Empty;
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
