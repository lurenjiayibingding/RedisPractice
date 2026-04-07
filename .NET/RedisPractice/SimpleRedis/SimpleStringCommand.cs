using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRedis
{
    /// <summary>
    /// 简单字符串相关的命令
    /// </summary>
    public class SimpleStringCommand
    {
        /// <summary>
        /// 异步的发送Set请求
        /// </summary>
        /// <param name="key">需要设置的Key</param>
        /// <param name="value">Key对应的Value</param>
        public static void Set(string key, string value)
        {

        }

        /// <summary>
        /// 异步的发送Get请求
        /// 并且将响应数据反序列化为对应的类型，若Key不存在则返回null
        /// </summary>
        /// <param name="key">需要查询的Key</param>
        /// <returns>Key对应的Value</returns>
        public static Task<T?> GetAsync<T>(string key)
        {
            return null;
        }
    }
}
