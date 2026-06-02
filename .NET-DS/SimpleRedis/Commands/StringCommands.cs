namespace SimpleRedis.Commands;

/// <summary>
/// Redis 字符串类型命令：SET / GET / INCR / DECR / APPEND / STRLEN / MGET / MSET / GETSET 等
/// </summary>
public class StringCommands : RedisCommand
{
    public StringCommands(IRedisClient client) : base(client) { }

    /// <summary>设置键值对</summary>
    public async Task<string> SetAsync(string key, string value, int? expirySeconds = null)
    {
        var args = expirySeconds.HasValue
            ? new[] { "SET", key, value, "EX", expirySeconds.Value.ToString() }
            : new[] { "SET", key, value };
        var result = await ExecuteAsync(args);
        return result.AsString()!;
    }

    /// <summary>获取键对应的值，不存在返回 null</summary>
    public async Task<string?> GetAsync(string key)
    {
        var result = await ExecuteAsync("GET", key);
        return result.IsNull() ? null : result.AsString();
    }

    /// <summary>设置新值并返回旧值</summary>
    public async Task<string?> GetSetAsync(string key, string value)
    {
        var result = await ExecuteAsync("GETSET", key, value);
        return result.IsNull() ? null : result.AsString();
    }

    /// <summary>键不存在时设置</summary>
    public async Task<bool> SetNxAsync(string key, string value)
    {
        var result = await ExecuteAsync("SETNX", key, value);
        return result.AsInt64() == 1;
    }

    /// <summary>设置键值对并指定过期秒数</summary>
    public async Task<string> SetExAsync(string key, int seconds, string value)
    {
        var result = await ExecuteAsync("SETEX", key, seconds.ToString(), value);
        return result.AsString()!;
    }

    /// <summary>批量设置多个键值对</summary>
    public async Task<string> MSetAsync(params (string key, string value)[] pairs)
    {
        var args = new List<string> { "MSET" };
        foreach (var (key, value) in pairs)
        {
            args.Add(key);
            args.Add(value);
        }
        var result = await ExecuteAsync(args.ToArray());
        return result.AsString()!;
    }

    /// <summary>批量获取多个键的值</summary>
    public async Task<string?[]> MGetAsync(params string[] keys)
    {
        var args = new[] { "MGET" }.Concat(keys).ToArray();
        var result = await ExecuteAsync(args);
        var arr = result.AsArray();
        return arr.Select(r => r.IsNull() ? null : r.AsString()).ToArray();
    }

    /// <summary>自增 1</summary>
    public async Task<long> IncrAsync(string key)
    {
        var result = await ExecuteAsync("INCR", key);
        return result.AsInt64();
    }

    /// <summary>自增指定数值</summary>
    public async Task<long> IncrByAsync(string key, long increment)
    {
        var result = await ExecuteAsync("INCRBY", key, increment.ToString());
        return result.AsInt64();
    }

    /// <summary>自增指定浮点数值</summary>
    public async Task<double> IncrByFloatAsync(string key, double increment)
    {
        var result = await ExecuteAsync("INCRBYFLOAT", key, increment.ToString("G"));
        return result.AsDouble();
    }

    /// <summary>自减 1</summary>
    public async Task<long> DecrAsync(string key)
    {
        var result = await ExecuteAsync("DECR", key);
        return result.AsInt64();
    }

    /// <summary>自减指定数值</summary>
    public async Task<long> DecrByAsync(string key, long decrement)
    {
        var result = await ExecuteAsync("DECRBY", key, decrement.ToString());
        return result.AsInt64();
    }

    /// <summary>追加字符串</summary>
    public async Task<long> AppendAsync(string key, string value)
    {
        var result = await ExecuteAsync("APPEND", key, value);
        return result.AsInt64();
    }

    /// <summary>获取字符串长度</summary>
    public async Task<long> StrLenAsync(string key)
    {
        var result = await ExecuteAsync("STRLEN", key);
        return result.AsInt64();
    }
}
