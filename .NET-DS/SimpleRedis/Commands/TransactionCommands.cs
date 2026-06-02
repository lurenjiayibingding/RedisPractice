namespace SimpleRedis.Commands;

/// <summary>
/// Redis 事务与管道命令：MULTI / EXEC / DISCARD / WATCH / UNWATCH / 管道
/// </summary>
public class TransactionCommands : RedisCommand
{
    public TransactionCommands(IRedisClient client) : base(client) { }

    /// <summary>开启事务</summary>
    public async Task<string> MultiAsync()
    {
        var result = await ExecuteAsync("MULTI");
        return result.AsString()!;
    }

    /// <summary>执行事务</summary>
    public async Task<RedisResult[]> ExecAsync()
    {
        var result = await ExecuteAsync("EXEC");
        return result.AsArray();
    }

    /// <summary>取消事务</summary>
    public async Task<string> DiscardAsync()
    {
        var result = await ExecuteAsync("DISCARD");
        return result.AsString()!;
    }

    /// <summary>监视一个或多个键</summary>
    public async Task<string> WatchAsync(params string[] keys)
    {
        var args = new[] { "WATCH" }.Concat(keys).ToArray();
        var result = await ExecuteAsync(args);
        return result.AsString()!;
    }

    /// <summary>取消监视</summary>
    public async Task<string> UnwatchAsync()
    {
        var result = await ExecuteAsync("UNWATCH");
        return result.AsString()!;
    }
}
