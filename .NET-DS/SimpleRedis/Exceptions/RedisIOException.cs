namespace SimpleRedis.Exceptions;

public class RedisIOException : Exception
{
    public RedisIOException(string message) : base(message) { }
    public RedisIOException(string message, Exception innerException)
        : base(message, innerException) { }
}
