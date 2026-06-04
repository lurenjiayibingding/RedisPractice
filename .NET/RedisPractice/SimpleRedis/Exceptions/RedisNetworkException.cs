namespace SimpleRedis.Exceptions
{
    public class RedisNetworkException : Exception
    {
        public RedisNetworkException(string message) : base(message)
        {
        }

        public RedisNetworkException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
