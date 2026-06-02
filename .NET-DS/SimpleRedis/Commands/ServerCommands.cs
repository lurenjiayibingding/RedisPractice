namespace SimpleRedis.Commands;

/// <summary>
/// Redis 服务器管理命令：PING / SELECT / FLUSHDB / INFO / CONFIG / DBSIZE / TIME 等
/// </summary>
public class ServerCommands : RedisCommand
{
    public ServerCommands(IRedisClient client) : base(client) { }

    /// <summary>检测连接是否存活</summary>
    public async Task<string> PingAsync()
    {
        var result = await ExecuteAsync("PING");
        return result.AsString()!;
    }

    /// <summary>切换数据库</summary>
    public async Task<string> SelectAsync(int dbNum)
    {
        var result = await ExecuteAsync("SELECT", dbNum.ToString());
        return result.AsString()!;
    }

    /// <summary>清空当前数据库</summary>
    public async Task<string> FlushDbAsync()
    {
        var result = await ExecuteAsync("FLUSHDB");
        return result.AsString()!;
    }

    /// <summary>清空所有数据库</summary>
    public async Task<string> FlushAllAsync()
    {
        var result = await ExecuteAsync("FLUSHALL");
        return result.AsString()!;
    }

    /// <summary>获取服务器信息</summary>
    public async Task<string> InfoAsync(string? section = null)
    {
        var args = section != null ? new[] { "INFO", section } : new[] { "INFO" };
        var result = await ExecuteAsync(args);
        return result.AsString()!;
    }

    /// <summary>获取配置参数</summary>
    public async Task<string[]> ConfigGetAsync(string parameter)
    {
        var result = await ExecuteAsync("CONFIG", "GET", parameter);
        return result.AsArray().Select(r => r.AsString()!).ToArray();
    }

    /// <summary>设置配置参数</summary>
    public async Task<string> ConfigSetAsync(string parameter, string value)
    {
        var result = await ExecuteAsync("CONFIG", "SET", parameter, value);
        return result.AsString()!;
    }

    /// <summary>获取当前数据库键数量</summary>
    public async Task<long> DBSizeAsync()
    {
        var result = await ExecuteAsync("DBSIZE");
        return result.AsInt64();
    }

    /// <summary>获取服务器时间</summary>
    public async Task<DateTime> TimeAsync()
    {
        var result = await ExecuteAsync("TIME");
        var arr = result.AsArray();
        var timestamp = long.Parse(arr[0].AsString()!);
        var microseconds = long.Parse(arr[1].AsString()!);
        return DateTimeOffset.FromUnixTimeSeconds(timestamp)
            .AddTicks(microseconds * 10) // 1 microsecond = 10 ticks
            .LocalDateTime;
    }

    /// <summary>获取客户端连接列表</summary>
    public async Task<string> ClientListAsync()
    {
        var result = await ExecuteAsync("CLIENT", "LIST");
        return result.AsString()!;
    }

    /// <summary>获取慢查询日志</summary>
    public async Task<string[]> SlowLogGetAsync(long count = 100)
    {
        var result = await ExecuteAsync("SLOWLOG", "GET", count.ToString());
        return result.AsArray().Select(r => r.ToString()).ToArray();
    }
}
