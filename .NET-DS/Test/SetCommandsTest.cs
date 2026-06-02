using SimpleRedis;
using SimpleRedis.Commands;
using Test.Mocks;

namespace Test;

[TestClass]
public class SetCommandsTest
{
    private MockRedisClient _mock = null!;
    private SetCommands _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _mock = new MockRedisClient();
        _sut = new SetCommands(_mock);
    }

    [TestMethod]
    public async Task SAddAsync_ReturnsAddedCount()
    {
        _mock.EnqueueInteger(3);
        var result = await _sut.SAddAsync("set", "a", "b", "c");
        Assert.AreEqual(3L, result);
        Assert.IsTrue(_mock.SentCommands[0].Contains("SADD"));
    }

    [TestMethod]
    public async Task SRemAsync_ReturnsRemovedCount()
    {
        _mock.EnqueueInteger(1);
        var result = await _sut.SRemAsync("set", "a");
        Assert.AreEqual(1L, result);
    }

    [TestMethod]
    public async Task SMembersAsync_ReturnsMembers()
    {
        _mock.EnqueueArray(new[]
        {
            RedisResult.BulkString("a"),
            RedisResult.BulkString("b")
        });
        var result = await _sut.SMembersAsync("set");
        Assert.AreEqual(2, result.Length);
    }

    [TestMethod]
    public async Task SIsMemberAsync_IsMember_ReturnsTrue()
    {
        _mock.EnqueueInteger(1);
        var result = await _sut.SIsMemberAsync("set", "a");
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task SCardAsync_ReturnsCount()
    {
        _mock.EnqueueInteger(5);
        var result = await _sut.SCardAsync("set");
        Assert.AreEqual(5L, result);
    }

    [TestMethod]
    public async Task SDiffAsync_SendsCorrectCommand()
    {
        _mock.EnqueueArray(Array.Empty<RedisResult>());
        await _sut.SDiffAsync("s1", "s2");
        Assert.IsTrue(_mock.SentCommands[0].Contains("SDIFF"));
    }

    [TestMethod]
    public async Task SInterAsync_SendsCorrectCommand()
    {
        _mock.EnqueueArray(Array.Empty<RedisResult>());
        await _sut.SInterAsync("s1", "s2");
        Assert.IsTrue(_mock.SentCommands[0].Contains("SINTER"));
    }

    [TestMethod]
    public async Task SUnionAsync_SendsCorrectCommand()
    {
        _mock.EnqueueArray(Array.Empty<RedisResult>());
        await _sut.SUnionAsync("s1", "s2");
        Assert.IsTrue(_mock.SentCommands[0].Contains("SUNION"));
    }

    [TestMethod]
    public async Task SMoveAsync_ReturnsTrue()
    {
        _mock.EnqueueInteger(1);
        var result = await _sut.SMoveAsync("src", "dst", "member");
        Assert.IsTrue(result);
    }
}
