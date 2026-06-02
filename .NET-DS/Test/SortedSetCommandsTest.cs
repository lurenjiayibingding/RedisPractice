using SimpleRedis;
using SimpleRedis.Commands;
using Test.Mocks;

namespace Test;

[TestClass]
public class SortedSetCommandsTest
{
    private MockRedisClient _mock = null!;
    private SortedSetCommands _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _mock = new MockRedisClient();
        _sut = new SortedSetCommands(_mock);
    }

    [TestMethod]
    public async Task ZAddAsync_ReturnsAddedCount()
    {
        _mock.EnqueueInteger(2);
        var result = await _sut.ZAddAsync("zset", (1.0, "a"), (2.0, "b"));
        Assert.AreEqual(2L, result);
        Assert.IsTrue(_mock.SentCommands[0].Contains("ZADD"));
    }

    [TestMethod]
    public async Task ZRemAsync_ReturnsRemovedCount()
    {
        _mock.EnqueueInteger(1);
        var result = await _sut.ZRemAsync("zset", "a");
        Assert.AreEqual(1L, result);
    }

    [TestMethod]
    public async Task ZScoreAsync_ReturnsScore()
    {
        _mock.EnqueueResult(RedisResult.Double(1.5));
        var result = await _sut.ZScoreAsync("zset", "a");
        Assert.IsNotNull(result);
        Assert.AreEqual(1.5, result!.Value, 0.001);
    }

    [TestMethod]
    public async Task ZScoreAsync_NotExist_ReturnsNull()
    {
        _mock.EnqueueResult(RedisResult.Null());
        var result = await _sut.ZScoreAsync("zset", "nonexistent");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ZIncrByAsync_ReturnsNewScore()
    {
        _mock.EnqueueResult(RedisResult.Double(2.5));
        var result = await _sut.ZIncrByAsync("zset", 1.5, "a");
        Assert.AreEqual(2.5, result, 0.001);
    }

    [TestMethod]
    public async Task ZCardAsync_ReturnsCount()
    {
        _mock.EnqueueInteger(100);
        var result = await _sut.ZCardAsync("zset");
        Assert.AreEqual(100L, result);
    }

    [TestMethod]
    public async Task ZRangeAsync_ReturnsMembers()
    {
        _mock.EnqueueArray(new[]
        {
            RedisResult.BulkString("a"),
            RedisResult.BulkString("b")
        });
        var result = await _sut.ZRangeAsync("zset", 0, -1);
        Assert.AreEqual(2, result.Length);
    }

    [TestMethod]
    public async Task ZRankAsync_ReturnsRank()
    {
        _mock.EnqueueInteger(0);
        var result = await _sut.ZRankAsync("zset", "a");
        Assert.AreEqual(0L, result);
    }

    [TestMethod]
    public async Task ZRankAsync_NotExist_ReturnsNull()
    {
        _mock.EnqueueResult(RedisResult.Null());
        var result = await _sut.ZRankAsync("zset", "nonexistent");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ZCountAsync_ReturnsCount()
    {
        _mock.EnqueueInteger(5);
        var result = await _sut.ZCountAsync("zset", "-inf", "+inf");
        Assert.AreEqual(5L, result);
    }
}
