namespace SimpleRedis.Exceptions;

public class RedisAuthenticationException : Exception
{
    public RedisAuthenticationException(string message) : base(message) { }
    public RedisAuthenticationException(string message, Exception innerException)
        : base(message, innerException) { }
}
