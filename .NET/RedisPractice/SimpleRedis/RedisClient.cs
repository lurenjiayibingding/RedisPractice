using SimpleRedis.Helper;
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
        /// <param name="password"></param>
        public RedisClient(string host, int port, string password)
        {
            _host = host;
            _port = port;
            _password = password;
        }

        public async Task ConnectAsync()
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(_host, _port);
            _stream = _tcpClient.GetStream();
        }

        /// <summary>
        /// 发送命令
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<T> SendCommandAsync<T>(string command)
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
                return (T)AnalysisRequest(byteArray);


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
            return await SendCommandAsync<string>("*1\r\n$4\r\nPING\r\n");
        }

        public async Task<string> SetAsync(string key, string value)
        {
            var command = $"set {key} {value}";
            return await SendCommandAsync<string>(TransitionCommand(command));
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var command = $"get {key}";
            return await SendCommandAsync<T>(TransitionCommand(command));
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

        public Object AnalysisRequest(byte[] bytes)
        {
            var firstChar = bytes[0];
            switch (firstChar)
            {
                case (byte)'+'://响应数据为简单字符串
                case (byte)'-'://响应数据为错误信息
                    return AnalysisSimpleOrErrorString(bytes);
                case (byte)':'://响应数据为整数
                    return AnalysisInteger(bytes);
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
        /// 解析简单字符串或者错误信息
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private long AnalysisInteger(byte[] bytes)
        {
            var result = Convert.ToInt64(Encoding.UTF8.GetString(bytes[1..^2]));
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
