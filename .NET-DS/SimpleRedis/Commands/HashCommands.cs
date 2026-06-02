namespace SimpleRedis.Commands;

/// <summary>
/// Redis 哈希类型命令：HSET / HGET / HDEL / HGETALL / HKEYS / HVALS 等
/// </summary>
public class HashCommands : RedisCommand
{
    public HashCommands(IRedisClient client) : base(client) { }

    /// <summary>设置哈希表字段值</summary>
    public async Task<bool> HSetAsync(string key, string field, string value)
    {
        var result = await ExecuteAsync("HSET", key, field, value);
        return result.AsInt64() == 1;
    }

    /// <summary>字段不存在时设置</summary>
    public async Task<bool> HSetNxAsync(string key, string field, string value)
    {
        var result = await ExecuteAsync("HSETNX", key, field, value);
        return result.AsInt64() == 1;
    }

    /// <summary>获取哈希表字段值</summary>
    public async Task<string?> HGetAsync(string key, string field)
    {
        var result = await ExecuteAsync("HGET", key, field);
        return result.IsNull() ? null : result.AsString();
    }

    /// <summary>获取所有字段和值</summary>
    public async Task<Dictionary<string, string>> HGetAllAsync(string key)
    {
        var result = await ExecuteAsync("HGETALL", key);
        var arr = result.AsArray();
        var dict = new Dictionary<string, string>(arr.Length / 2);
        for (var i = 0; i + 1 < arr.Length; i += 2)
        {
            dict[arr[i].AsString()!] = arr[i + 1].AsString()!;
        }
        return dict;
    }

    /// <summary>删除一个或多个字段</summary>
    public async Task<long> HDelAsync(string key, params string[] fields)
    {
        var args = new[] { "HDEL", key }.Concat(fields).ToArray();
        var result = await ExecuteAsync(args);
        return result.AsInt64();
    }

    /// <summary>判断字段是否存在</summary>
    public async Task<bool> HExistsAsync(string key, string field)
    {
        var result = await ExecuteAsync("HEXISTS", key, field);
        return result.AsInt64() == 1;
    }

    /// <summary>获取所有字段名</summary>
    public async Task<string[]> HKeysAsync(string key)
    {
        var result = await ExecuteAsync("HKEYS", key);
        return result.AsArray().Select(r => r.AsString()!).ToArray();
    }

    /// <summary>获取所有值</summary>
    public async Task<string[]> HValsAsync(string key)
    {
        var result = await ExecuteAsync("HVALS", key);
        return result.AsArray().Select(r => r.AsString()!).ToArray();
    }

    /// <summary>获取字段数量</summary>
    public async Task<long> HLenAsync(string key)
    {
        var result = await ExecuteAsync("HLEN", key);
        return result.AsInt64();
    }

    /// <summary>获取字段值的字符串长度</summary>
    public async Task<long> HStrLenAsync(string key, string field)
    {
        var result = await ExecuteAsync("HSTRLEN", key, field);
        return result.AsInt64();
    }

    /// <summary>自增字段数值</summary>
    public async Task<long> HIncrByAsync(string key, string field, long increment)
    {
        var result = await ExecuteAsync("HINCRBY", key, field, increment.ToString());
        return result.AsInt64();
    }

    /// <summary>获取多个字段的值</summary>
    public async Task<string?[]> HMGetAsync(string key, params string[] fields)
    {
        var args = new[] { "HMGET", key }.Concat(fields).ToArray();
        var result = await ExecuteAsync(args);
        return result.AsArray().Select(r => r.IsNull() ? null : r.AsString()).ToArray();
    }
}
