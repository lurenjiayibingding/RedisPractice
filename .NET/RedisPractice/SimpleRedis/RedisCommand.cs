using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRedis
{
    /// <summary>
    /// Redis命令类
    /// </summary>
    public class RedisCommand
    {
        private readonly RedisClient _clien;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="client"></param>
        public RedisCommand(RedisClient client)
        {
            _clien = client;
        }

        /// <summary>
        /// 异步的发送Ping命令
        /// </summary>
        /// <returns></returns>
        public async Task<string> PingAsync()
        {
            return await _clien.SendCommandAsync("*1\r\n$4\r\nPING\r\n");
        }

        /// <summary>
        /// 异步的发送Set命令，设置键值对
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public async Task<string> SetAsync(string key, string value)
        {
            var command = $"set {key} {value}";
            return await _clien.SendCommandAsync(_clien.TransitionCommand(command));
        }

        /// <summary>
        /// 异步的发送Get命令，获取键对应的值
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>键对应的值</returns>
        public async Task<string> GetAsync(string key)
        {
            var command = $"get {key}";
            return await _clien.SendCommandAsync(_clien.TransitionCommand(command));
        }

        /// <summary>
        /// 异步的发送Incr命令
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public async Task<long> IncrAsync(string key)
        {
            var command = $"incr {key}";
            return Convert.ToInt64(await _clien.SendCommandAsync(_clien.TransitionCommand(command)));
        }

        /// <summary>
        /// 异步的发送Select命令
        /// </summary>
        /// <param name="dbNum">需要切换的Db编号</param>
        /// <returns>切换数据库</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public async Task<string> SelectAsync(int dbNum)
        {   
            if (dbNum < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dbNum), "数据库编号不能为负数");
            }
            if (dbNum == _clien._db)
            {
                return "OK";
            }
            var command = $"select {dbNum}";
            var result = await _clien.SendCommandAsync(_clien.TransitionCommand(command));
            if (string.Equals(result, "ok", StringComparison.InvariantCultureIgnoreCase))
            {
                _clien._db = dbNum;
            }
            return result;
        }
    }
}
