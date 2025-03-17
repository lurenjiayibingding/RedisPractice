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
        public async Task<string> SendCommandAsync(string command)
        {
            var sendBuffer = Encoding.UTF8.GetBytes(command);
            await _stream.WriteAsync(sendBuffer, 0, sendBuffer.Length);

            using var memoryStream = new MemoryStream();
            var receiveBuffer = new byte[1024];

            while (true)
            {
                var readLength = await _stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);
                memoryStream.Write(receiveBuffer, 0, readLength);
                if (receiveBuffer[readLength - 2] == '\r' && receiveBuffer[readLength - 1] == '\n')
                {
                    break;
                }
            }

            // do
            // {
            //     var await _stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);
            //     memoryStream.Write(receiveBuffer, 0, receiveBuffer.Length);
            // }
            // while (!_stream.DataAvailable);

            var result = Encoding.UTF8.GetString(memoryStream.ToArray());
            return result;
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

        public string AnalysisRequest(string request)
        {
            var firstChar = request[0];
            switch (firstChar)
            {
                case '+'://响应数据为简单字符串
                    break;
                case '-'://响应数据为错误信息
                    break;
                case ':'://响应数据为整数
                    break;
                case '$'://响应数据为批量字符串
                    break;
                case '*'://响应数据为数组
                    break;
                default:
                    break;
            }
            return string.Empty;
        }
    }
}
