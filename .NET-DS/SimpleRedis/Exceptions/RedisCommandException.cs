namespace SimpleRedis.Exceptions;

public class RedisCommandException : Exception
{
    public RedisCommandException(string message) : base(message) { }
    public RedisCommandException(string message, Exception innerException)
        : base(message, innerException) { }
}
