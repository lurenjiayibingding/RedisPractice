namespace SimpleRedis.Exceptions;

public class RedisServerException : Exception
{
    public RedisServerException(string message) : base(message) { }
    public RedisServerException(string message, Exception innerException)
        : base(message, innerException) { }
}
