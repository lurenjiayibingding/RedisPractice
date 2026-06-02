namespace SimpleRedis.Commands;

/// <summary>
/// Redis 有序集合类型命令：ZADD / ZREM / ZSCORE / ZRANGE / ZRANK / ZCOUNT 等
/// </summary>
public class SortedSetCommands : RedisCommand
{
    public SortedSetCommands(IRedisClient client) : base(client) { }

    /// <summary>添加一个或多个成员（带分数）</summary>
    public async Task<long> ZAddAsync(string key, params (double score, string member)[] scoredMembers)
    {
        var args = new List<string> { "ZADD", key };
        foreach (var (score, member) in scoredMembers)
        {
            args.Add(score.ToString("G"));
            args.Add(member);
        }
        var result = await ExecuteAsync(args.ToArray());
        return result.AsInt64();
    }

    /// <summary>移除一个或多个成员</summary>
    public async Task<long> ZRemAsync(string key, params string[] members)
    {
        var args = new[] { "ZREM", key }.Concat(members).ToArray();
        var result = await ExecuteAsync(args);
        return result.AsInt64();
    }

    /// <summary>获取成员分数</summary>
    public async Task<double?> ZScoreAsync(string key, string member)
    {
        var result = await ExecuteAsync("ZSCORE", key, member);
        return result.IsNull() ? null : result.AsDouble();
    }

    /// <summary>自增成员分数</summary>
    public async Task<double> ZIncrByAsync(string key, double increment, string member)
    {
        var result = await ExecuteAsync("ZINCRBY", key, increment.ToString("G"), member);
        return result.AsDouble();
    }

    /// <summary>获取成员数量</summary>
    public async Task<long> ZCardAsync(string key)
    {
        var result = await ExecuteAsync("ZCARD", key);
        return result.AsInt64();
    }

    /// <summary>统计分数区间内的成员数</summary>
    public async Task<long> ZCountAsync(string key, string min, string max)
    {
        var result = await ExecuteAsync("ZCOUNT", key, min, max);
        return result.AsInt64();
    }

    /// <summary>按索引范围获取成员</summary>
    public async Task<string[]> ZRangeAsync(string key, long start, long stop, bool withScores = false)
    {
        var args = withScores
            ? new[] { "ZRANGE", key, start.ToString(), stop.ToString(), "WITHSCORES" }
            : new[] { "ZRANGE", key, start.ToString(), stop.ToString() };
        var result = await ExecuteAsync(args);
        return result.AsArray().Select(r => r.AsString()!).ToArray();
    }

    /// <summary>按分数范围获取成员</summary>
    public async Task<string[]> ZRangeByScoreAsync(string key, string min, string max, bool withScores = false)
    {
        var args = withScores
            ? new[] { "ZRANGEBYSCORE", key, min, max, "WITHSCORES" }
            : new[] { "ZRANGEBYSCORE", key, min, max };
        var result = await ExecuteAsync(args);
        return result.AsArray().Select(r => r.AsString()!).ToArray();
    }

    /// <summary>获取成员排名（分数从低到高）</summary>
    public async Task<long?> ZRankAsync(string key, string member)
    {
        var result = await ExecuteAsync("ZRANK", key, member);
        return result.IsNull() ? null : result.AsInt64();
    }

    /// <summary>获取成员排名（分数从高到低）</summary>
    public async Task<long?> ZRevRankAsync(string key, string member)
    {
        var result = await ExecuteAsync("ZREVRANK", key, member);
        return result.IsNull() ? null : result.AsInt64();
    }

    /// <summary>按排名范围移除</summary>
    public async Task<long> ZRemRangeByRankAsync(string key, long start, long stop)
    {
        var result = await ExecuteAsync("ZREMRANGEBYRANK", key, start.ToString(), stop.ToString());
        return result.AsInt64();
    }

    /// <summary>按分数范围移除</summary>
    public async Task<long> ZRemRangeByScoreAsync(string key, string min, string max)
    {
        var result = await ExecuteAsync("ZREMRANGEBYSCORE", key, min, max);
        return result.AsInt64();
    }

    /// <summary>交集并存储</summary>
    public async Task<long> ZInterStoreAsync(string destination, params string[] keys)
    {
        var args = new[] { "ZINTERSTORE", destination, keys.Length.ToString() }.Concat(keys).ToArray();
        var result = await ExecuteAsync(args);
        return result.AsInt64();
    }

    /// <summary>并集并存储</summary>
    public async Task<long> ZUnionStoreAsync(string destination, params string[] keys)
    {
        var args = new[] { "ZUNIONSTORE", destination, keys.Length.ToString() }.Concat(keys).ToArray();
        var result = await ExecuteAsync(args);
        return result.AsInt64();
    }
}
