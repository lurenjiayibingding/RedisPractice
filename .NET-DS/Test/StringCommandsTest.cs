using SimpleRedis;
using SimpleRedis.Commands;
using Test.Mocks;

namespace Test;

[TestClass]
public class StringCommandsTest
{
    private MockRedisClient _mock = null!;
    private StringCommands _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _mock = new MockRedisClient();
        _sut = new StringCommands(_mock);
    }

    [TestMethod]
    public async Task SetAsync_SendsCorrectCommand()
    {
        _mock.EnqueueOK();
        var result = await _sut.SetAsync("name", "Tom");
        Assert.AreEqual("OK", result);
        Assert.IsTrue(_mock.SentCommands[0].Contains("*3\r\n$3\r\nSET\r\n$4\r\nname\r\n$3\r\nTom\r\n"));
    }

    [TestMethod]
    public async Task SetAsync_WithExpiry_IncludesExSecondss()
    {
        _mock.EnqueueOK();
        await _sut.SetAsync("key", "value", 60);
        Assert.IsTrue(_mock.SentCommands[0].Contains("EX"));
        Assert.IsTrue(_mock.SentCommands[0].Contains("60"));
    }

    [TestMethod]
    public async Task GetAsync_ReturnsValue()
    {
        _mock.EnqueueBulkString("hello");
        var result = await _sut.GetAsync("mykey");
        Assert.AreEqual("hello", result);
        Assert.IsTrue(_mock.SentCommands[0].Contains("GET"));
    }

    [TestMethod]
    public async Task GetAsync_KeyNotExist_ReturnsNull()
    {
        _mock.EnqueueResult(RedisResult.Null());
        var result = await _sut.GetAsync("nonexistent");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task IncrAsync_ReturnsLong()
    {
        _mock.EnqueueInteger(5);
        var result = await _sut.IncrAsync("counter");
        Assert.AreEqual(5L, result);
        Assert.IsTrue(_mock.SentCommands[0].Contains("INCR"));
    }

    [TestMethod]
    public async Task IncrByAsync_ReturnsIncrementedValue()
    {
        _mock.EnqueueInteger(10);
        var result = await _sut.IncrByAsync("counter", 5);
        Assert.AreEqual(10L, result);
    }

    [TestMethod]
    public async Task DecrAsync_ReturnsDecrementedValue()
    {
        _mock.EnqueueInteger(4);
        var result = await _sut.DecrAsync("counter");
        Assert.AreEqual(4L, result);
    }

    [TestMethod]
    public async Task SetNxAsync_KeyNotExist_ReturnsTrue()
    {
        _mock.EnqueueInteger(1);
        var result = await _sut.SetNxAsync("newkey", "val");
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task SetNxAsync_KeyExists_ReturnsFalse()
    {
        _mock.EnqueueInteger(0);
        var result = await _sut.SetNxAsync("existing", "val");
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task MGetAsync_ReturnsArray()
    {
        _mock.EnqueueArray(new[] { RedisResult.BulkString("v1"), RedisResult.BulkString("v2") });
        var result = await _sut.MGetAsync("k1", "k2");
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual("v1", result[0]);
        Assert.AreEqual("v2", result[1]);
    }

    [TestMethod]
    public async Task MGetAsync_WithNull_ReturnsNullElement()
    {
        _mock.EnqueueArray(new RedisResult[] { RedisResult.BulkString("v1"), RedisResult.Null() });
        var result = await _sut.MGetAsync("k1", "k2");
        Assert.AreEqual("v1", result[0]);
        Assert.IsNull(result[1]);
    }

    [TestMethod]
    public async Task MSetAsync_SendsCorrectCommand()
    {
        _mock.EnqueueOK();
        await _sut.MSetAsync(("k1", "v1"), ("k2", "v2"));
        var sent = _mock.SentCommands[0];
        Assert.IsTrue(sent.Contains("MSET"));
        Assert.IsTrue(sent.Contains("k1"));
        Assert.IsTrue(sent.Contains("v1"));
        Assert.IsTrue(sent.Contains("k2"));
        Assert.IsTrue(sent.Contains("v2"));
    }

    [TestMethod]
    public async Task AppendAsync_ReturnsNewLength()
    {
        _mock.EnqueueInteger(8);
        var result = await _sut.AppendAsync("key", "world");
        Assert.AreEqual(8L, result);
    }

    [TestMethod]
    public async Task StrLenAsync_ReturnsLength()
    {
        _mock.EnqueueInteger(5);
        var result = await _sut.StrLenAsync("key");
        Assert.AreEqual(5L, result);
    }

    [TestMethod]
    public async Task GetSetAsync_ReturnsOldValue()
    {
        _mock.EnqueueBulkString("old");
        var result = await _sut.GetSetAsync("key", "new");
        Assert.AreEqual("old", result);
    }

    [TestMethod]
    public async Task IncrByFloatAsync_ReturnsDouble()
    {
        _mock.EnqueueResult(RedisResult.Double(3.5));
        var result = await _sut.IncrByFloatAsync("key", 1.5);
        Assert.AreEqual(3.5, result, 0.001);
    }
}
