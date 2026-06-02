namespace SimpleRedis;

/// <summary>
/// Redis 命令基类，提供发送命令的通用基础设施
/// </summary>
public abstract class RedisCommand
{
    protected IRedisClient Client { get; }

    protected RedisCommand(IRedisClient client)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <summary>
    /// 构造 RESP 命令并发送
    /// </summary>
    protected Task<RedisResult> ExecuteAsync(params string[] args)
    {
        var resp = RESPEncoder.EncodeArgs(args);
        return Client.SendCommandAsync(resp);
    }
}
