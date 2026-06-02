namespace SimpleRedis.Commands;

/// <summary>
/// Redis 通用键命令：DEL / EXISTS / EXPIRE / TTL / TYPE / RENAME / SCAN 等
/// </summary>
public class KeyCommands : RedisCommand
{
    public KeyCommands(IRedisClient client) : base(client) { }

    /// <summary>删除一个或多个键</summary>
    public async Task<long> DelAsync(params string[] keys)
    {
        var args = new[] { "DEL" }.Concat(keys).ToArray();
        var result = await ExecuteAsync(args);
        return result.AsInt64();
    }

    /// <summary>判断键是否存在</summary>
    public async Task<long> ExistsAsync(params string[] keys)
    {
        var args = new[] { "EXISTS" }.Concat(keys).ToArray();
        var result = await ExecuteAsync(args);
        return result.AsInt64();
    }

    /// <summary>设置过期时间（秒）</summary>
    public async Task<bool> ExpireAsync(string key, long seconds)
    {
        var result = await ExecuteAsync("EXPIRE", key, seconds.ToString());
        return result.AsInt64() == 1;
    }

    /// <summary>设置过期时间戳（秒级 Unix 时间戳）</summary>
    public async Task<bool> ExpireAtAsync(string key, long timestamp)
    {
        var result = await ExecuteAsync("EXPIREAT", key, timestamp.ToString());
        return result.AsInt64() == 1;
    }

    /// <summary>获取剩余过期时间（秒），-1 表示无过期时间，-2 表示键不存在</summary>
    public async Task<long> TTLAsync(string key)
    {
        var result = await ExecuteAsync("TTL", key);
        return result.AsInt64();
    }

    /// <summary>获取剩余过期时间（毫秒）</summary>
    public async Task<long> PTTLAsync(string key)
    {
        var result = await ExecuteAsync("PTTL", key);
        return result.AsInt64();
    }

    /// <summary>移除过期时间</summary>
    public async Task<bool> PersistAsync(string key)
    {
        var result = await ExecuteAsync("PERSIST", key);
        return result.AsInt64() == 1;
    }

    /// <summary>获取键的类型</summary>
    public async Task<string> TypeAsync(string key)
    {
        var result = await ExecuteAsync("TYPE", key);
        return result.AsString()!;
    }

    /// <summary>重命名键</summary>
    public async Task<string> RenameAsync(string key, string newKey)
    {
        var result = await ExecuteAsync("RENAME", key, newKey);
        return result.AsString()!;
    }

    /// <summary>新键不存在时重命名</summary>
    public async Task<bool> RenameNxAsync(string key, string newKey)
    {
        var result = await ExecuteAsync("RENAMENX", key, newKey);
        return result.AsInt64() == 1;
    }

    /// <summary>扫描键空间</summary>
    public async Task<(long cursor, string[] keys)> ScanAsync(long cursor, string? pattern = null, long? count = null)
    {
        var args = new List<string> { "SCAN", cursor.ToString() };
        if (pattern != null) { args.Add("MATCH"); args.Add(pattern); }
        if (count.HasValue) { args.Add("COUNT"); args.Add(count.Value.ToString()); }

        var result = await ExecuteAsync(args.ToArray());
        var arr = result.AsArray();
        var nextCursor = long.Parse(arr[0].AsString()!);
        var keys = arr[1].AsArray().Select(r => r.AsString()!).ToArray();
        return (nextCursor, keys);
    }
}
