using SimpleRedis;
using SimpleRedis.Commands;
using Test.Mocks;

namespace Test;

[TestClass]
public class ServerCommandsTest
{
    private MockRedisClient _mock = null!;
    private ServerCommands _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _mock = new MockRedisClient();
        _sut = new ServerCommands(_mock);
    }

    [TestMethod]
    public async Task PingAsync_SendsCorrectCommand()
    {
        _mock.EnqueueResult(RedisResult.SimpleString("PONG"));
        var result = await _sut.PingAsync();
        Assert.AreEqual("PONG", result);
        Assert.IsTrue(_mock.SentCommands[0].Contains("*1\r\n$4\r\nPING\r\n"));
    }

    [TestMethod]
    public async Task SelectAsync_SendsCorrectCommand()
    {
        _mock.EnqueueOK();
        var result = await _sut.SelectAsync(1);
        Assert.AreEqual("OK", result);
        Assert.IsTrue(_mock.SentCommands[0].Contains("SELECT"));
        Assert.IsTrue(_mock.SentCommands[0].Contains("1"));
    }

    [TestMethod]
    public async Task FlushDbAsync_SendsCorrectCommand()
    {
        _mock.EnqueueOK();
        var result = await _sut.FlushDbAsync();
        Assert.AreEqual("OK", result);
        Assert.IsTrue(_mock.SentCommands[0].Contains("FLUSHDB"));
    }

    [TestMethod]
    public async Task FlushAllAsync_SendsCorrectCommand()
    {
        _mock.EnqueueOK();
        var result = await _sut.FlushAllAsync();
        Assert.AreEqual("OK", result);
        Assert.IsTrue(_mock.SentCommands[0].Contains("FLUSHALL"));
    }

    [TestMethod]
    public async Task InfoAsync_WithoutSection_SendsInfoOnly()
    {
        _mock.EnqueueBulkString("redis_version:7.0");
        var result = await _sut.InfoAsync();
        Assert.IsNotNull(result);
        Assert.AreEqual("*1\r\n$4\r\nINFO\r\n", _mock.SentCommands[0]);
    }

    [TestMethod]
    public async Task InfoAsync_WithSection_SendsSection()
    {
        _mock.EnqueueBulkString("used_memory:1000");
        var result = await _sut.InfoAsync("memory");
        Assert.IsNotNull(result);
        Assert.IsTrue(_mock.SentCommands[0].Contains("memory"));
    }

    [TestMethod]
    public async Task DBSizeAsync_ReturnsCount()
    {
        _mock.EnqueueInteger(100);
        var result = await _sut.DBSizeAsync();
        Assert.AreEqual(100L, result);
    }

    [TestMethod]
    public async Task TimeAsync_ReturnsDateTime()
    {
        // Redis TIME 返回 [timestamp_seconds, microseconds]
        _mock.EnqueueArray(new[]
        {
            RedisResult.BulkString("1700000000"),
            RedisResult.BulkString("500000")
        });
        var result = await _sut.TimeAsync();
        Assert.IsTrue(result.Year >= 2023);
    }

    [TestMethod]
    public async Task ConfigGetAsync_SendsCorrectCommand()
    {
        _mock.EnqueueArray(new[] { RedisResult.BulkString("timeout"), RedisResult.BulkString("300") });
        var result = await _sut.ConfigGetAsync("timeout");
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual("timeout", result[0]);
        Assert.AreEqual("300", result[1]);
    }
}
