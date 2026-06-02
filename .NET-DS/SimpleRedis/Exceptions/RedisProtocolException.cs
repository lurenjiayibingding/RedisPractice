namespace SimpleRedis.Exceptions;

public class RedisProtocolException : Exception
{
    public RedisProtocolException(string message) : base(message) { }
    public RedisProtocolException(string message, Exception innerException)
        : base(message, innerException) { }
}
