namespace SimpleRedis.Commands;

/// <summary>
/// Redis 列表类型命令：LPUSH / RPUSH / LPOP / RPOP / LLEN / LRANGE / LINDEX 等
/// </summary>
public class ListCommands : RedisCommand
{
    public ListCommands(IRedisClient client) : base(client) { }

    /// <summary>从左侧推入一个或多个元素</summary>
    public async Task<long> LPushAsync(string key, params string[] elements)
    {
        var args = new[] { "LPUSH", key }.Concat(elements).ToArray();
        var result = await ExecuteAsync(args);
        return result.AsInt64();
    }

    /// <summary>从右侧推入一个或多个元素</summary>
    public async Task<long> RPushAsync(string key, params string[] elements)
    {
        var args = new[] { "RPUSH", key }.Concat(elements).ToArray();
        var result = await ExecuteAsync(args);
        return result.AsInt64();
    }

    /// <summary>从左侧弹出元素</summary>
    public async Task<string?> LPopAsync(string key)
    {
        var result = await ExecuteAsync("LPOP", key);
        return result.IsNull() ? null : result.AsString();
    }

    /// <summary>从右侧弹出元素</summary>
    public async Task<string?> RPopAsync(string key)
    {
        var result = await ExecuteAsync("RPOP", key);
        return result.IsNull() ? null : result.AsString();
    }

    /// <summary>移除指定数量的元素</summary>
    public async Task<long> LRemAsync(string key, long count, string element)
    {
        var result = await ExecuteAsync("LREM", key, count.ToString(), element);
        return result.AsInt64();
    }

    /// <summary>获取列表长度</summary>
    public async Task<long> LLenAsync(string key)
    {
        var result = await ExecuteAsync("LLEN", key);
        return result.AsInt64();
    }

    /// <summary>获取指定索引的元素</summary>
    public async Task<string?> LIndexAsync(string key, long index)
    {
        var result = await ExecuteAsync("LINDEX", key, index.ToString());
        return result.IsNull() ? null : result.AsString();
    }

    /// <summary>设置指定索引的元素值</summary>
    public async Task<string> LSetAsync(string key, long index, string element)
    {
        var result = await ExecuteAsync("LSET", key, index.ToString(), element);
        return result.AsString()!;
    }

    /// <summary>获取指定范围的元素</summary>
    public async Task<string[]> LRangeAsync(string key, long start, long stop)
    {
        var result = await ExecuteAsync("LRANGE", key, start.ToString(), stop.ToString());
        return result.AsArray().Select(r => r.AsString()!).ToArray();
    }

    /// <summary>在指定元素前插入</summary>
    public async Task<long> LInsertBeforeAsync(string key, string pivot, string element)
    {
        var result = await ExecuteAsync("LINSERT", key, "BEFORE", pivot, element);
        return result.AsInt64();
    }

    /// <summary>在指定元素后插入</summary>
    public async Task<long> LInsertAfterAsync(string key, string pivot, string element)
    {
        var result = await ExecuteAsync("LINSERT", key, "AFTER", pivot, element);
        return result.AsInt64();
    }

    /// <summary>修剪列表，只保留指定范围内的元素</summary>
    public async Task<string> LTrimAsync(string key, long start, long stop)
    {
        var result = await ExecuteAsync("LTRIM", key, start.ToString(), stop.ToString());
        return result.AsString()!;
    }
}
