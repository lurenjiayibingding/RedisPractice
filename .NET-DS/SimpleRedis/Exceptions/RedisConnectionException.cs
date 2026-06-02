namespace SimpleRedis.Exceptions;

public class RedisConnectionException : Exception
{
    public RedisConnectionException(string message) : base(message) { }
    public RedisConnectionException(string message, Exception innerException)
        : base(message, innerException) { }
}
