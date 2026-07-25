using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRedis.Protocol
{
    /// <summary>
    /// RESP协议解析器
    /// </summary>
    public class RespParser
    {
        /// <summary>
        /// 将输入的命令转换为redis协议
        /// </summary>
        /// <param name="command">输入的命令</param>
        /// <returns>转换为符合Redis协议的命令</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string TransitionCommand(string command)
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
        public static RespValue AnalysisRequest<T>(byte[] bytes)
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
        private static RespSimpleString AnalysisSimpleOrErrorString(byte[] bytes)
        {
            var result = Encoding.UTF8.GetString(bytes[1..^2]);
            return new RespSimpleString(result);
        }

        /// <summary>
        /// 解析批量字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private static RespSimpleString AnalysisBatchString(byte[] bytes)
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
                return new RespSimpleString(string.Empty);
            }
            return new RespSimpleString(Encoding.UTF8.GetString(bytes.AsSpan(lengthEndIndex + 3, stringLength)));
        }
    }
}
