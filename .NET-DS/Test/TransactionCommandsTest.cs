using SimpleRedis;
using SimpleRedis.Commands;
using Test.Mocks;

namespace Test;

[TestClass]
public class TransactionCommandsTest
{
    private MockRedisClient _mock = null!;
    private TransactionCommands _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _mock = new MockRedisClient();
        _sut = new TransactionCommands(_mock);
    }

    [TestMethod]
    public async Task MultiAsync_SendsCorrectCommand()
    {
        _mock.EnqueueOK();
        var result = await _sut.MultiAsync();
        Assert.AreEqual("OK", result);
        Assert.IsTrue(_mock.SentCommands[0].Contains("MULTI"));
    }

    [TestMethod]
    public async Task ExecAsync_ReturnsResults()
    {
        _mock.EnqueueArray(new[]
        {
            RedisResult.SimpleString("OK"),
            RedisResult.BulkString("value")
        });
        var result = await _sut.ExecAsync();
        Assert.AreEqual(2, result.Length);
    }

    [TestMethod]
    public async Task DiscardAsync_SendsCorrectCommand()
    {
        _mock.EnqueueOK();
        var result = await _sut.DiscardAsync();
        Assert.AreEqual("OK", result);
        Assert.IsTrue(_mock.SentCommands[0].Contains("DISCARD"));
    }

    [TestMethod]
    public async Task WatchAsync_SendsCorrectCommand()
    {
        _mock.EnqueueOK();
        var result = await _sut.WatchAsync("key1", "key2");
        Assert.AreEqual("OK", result);
        Assert.IsTrue(_mock.SentCommands[0].Contains("WATCH"));
        Assert.IsTrue(_mock.SentCommands[0].Contains("key1"));
    }

    [TestMethod]
    public async Task UnwatchAsync_SendsCorrectCommand()
    {
        _mock.EnqueueOK();
        var result = await _sut.UnwatchAsync();
        Assert.AreEqual("OK", result);
        Assert.IsTrue(_mock.SentCommands[0].Contains("UNWATCH"));
    }
}
