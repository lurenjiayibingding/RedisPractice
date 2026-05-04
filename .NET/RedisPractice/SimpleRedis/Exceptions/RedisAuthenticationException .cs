using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRedis.Exceptions
{
    public class RedisAuthenticationException : Exception
    {
        public RedisAuthenticationException(string message) : base(message)
        {
            
        }

        public RedisAuthenticationException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
