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

            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<string> PingAsync()
        {
            return await SendCommandAsync("*1\r\n$4\r\nPING\r\n");
        }
    }
}
