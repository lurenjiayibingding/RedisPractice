namespace SimpleRedis.Commands;

/// <summary>
/// Redis 集合类型命令：SADD / SREM / SMEMBERS / SISMEMBER / SCARD / SPOP / SDIFF / SINTER / SUNION 等
/// </summary>
public class SetCommands : RedisCommand
{
    public SetCommands(IRedisClient client) : base(client) { }

    /// <summary>添加一个或多个成员</summary>
    public async Task<long> SAddAsync(string key, params string[] members)
    {
        var args = new[] { "SADD", key }.Concat(members).ToArray();
        var result = await ExecuteAsync(args);
        return result.AsInt64();
    }

    /// <summary>移除一个或多个成员</summary>
    public async Task<long> SRemAsync(string key, params string[] members)
    {
        var args = new[] { "SREM", key }.Concat(members).ToArray();
        var result = await ExecuteAsync(args);
        return result.AsInt64();
    }

    /// <summary>获取所有成员</summary>
    public async Task<string[]> SMembersAsync(string key)
    {
        var result = await ExecuteAsync("SMEMBERS", key);
        return result.AsArray().Select(r => r.AsString()!).ToArray();
    }

    /// <summary>判断成员是否存在</summary>
    public async Task<bool> SIsMemberAsync(string key, string member)
    {
        var result = await ExecuteAsync("SISMEMBER", key, member);
        return result.AsInt64() == 1;
    }

    /// <summary>获取成员数量</summary>
    public async Task<long> SCardAsync(string key)
    {
        var result = await ExecuteAsync("SCARD", key);
        return result.AsInt64();
    }

    /// <summary>随机移除并返回一个或多个成员</summary>
    public async Task<string?> SPopAsync(string key)
    {
        var result = await ExecuteAsync("SPOP", key);
        return result.IsNull() ? null : result.AsString();
    }

    /// <summary>随机获取一个或多个成员</summary>
    public async Task<string[]> SRandMemberAsync(string key, long count)
    {
        var result = await ExecuteAsync("SRANDMEMBER", key, count.ToString());
        return result.AsArray().Select(r => r.AsString()!).ToArray();
    }

    /// <summary>差集</summary>
    public async Task<string[]> SDiffAsync(params string[] keys)
    {
        var args = new[] { "SDIFF" }.Concat(keys).ToArray();
        var result = await ExecuteAsync(args);
        return result.AsArray().Select(r => r.AsString()!).ToArray();
    }

    /// <summary>交集</summary>
    public async Task<string[]> SInterAsync(params string[] keys)
    {
        var args = new[] { "SINTER" }.Concat(keys).ToArray();
        var result = await ExecuteAsync(args);
        return result.AsArray().Select(r => r.AsString()!).ToArray();
    }

    /// <summary>并集</summary>
    public async Task<string[]> SUnionAsync(params string[] keys)
    {
        var args = new[] { "SUNION" }.Concat(keys).ToArray();
        var result = await ExecuteAsync(args);
        return result.AsArray().Select(r => r.AsString()!).ToArray();
    }

    /// <summary>移动成员到另一个集合</summary>
    public async Task<bool> SMoveAsync(string source, string destination, string member)
    {
        var result = await ExecuteAsync("SMOVE", source, destination, member);
        return result.AsInt64() == 1;
    }

    /// <summary>差集并存储</summary>
    public async Task<long> SDiffStoreAsync(string destination, params string[] keys)
    {
        var args = new[] { "SDIFFSTORE", destination }.Concat(keys).ToArray();
        var result = await ExecuteAsync(args);
        return result.AsInt64();
    }

    /// <summary>交集并存储</summary>
    public async Task<long> SInterStoreAsync(string destination, params string[] keys)
    {
        var args = new[] { "SINTERSTORE", destination }.Concat(keys).ToArray();
        var result = await ExecuteAsync(args);
        return result.AsInt64();
    }

    /// <summary>并集并存储</summary>
    public async Task<long> SUnionStoreAsync(string destination, params string[] keys)
    {
        var args = new[] { "SUNIONSTORE", destination }.Concat(keys).ToArray();
        var result = await ExecuteAsync(args);
        return result.AsInt64();
    }
}
