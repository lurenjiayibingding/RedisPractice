using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRedis.Protocol
{
    /// <summary>
    /// RESP协议响应数据的简单字符串类型
    /// </summary>
    public sealed class RespSimpleString : RespValue
    {
        public string Value { get; }

        public RespSimpleString(string value)
        {
            Value = value;
        }
    }
}