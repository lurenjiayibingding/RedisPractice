namespace SimpleRedis.Exceptions;

public class RedisTimeoutException : Exception
{
    public RedisTimeoutException(string message) : base(message) { }
    public RedisTimeoutException(string message, Exception innerException)
        : base(message, innerException) { }
}
