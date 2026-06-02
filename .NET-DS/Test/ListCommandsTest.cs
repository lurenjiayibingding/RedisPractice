using SimpleRedis;
using SimpleRedis.Commands;
using Test.Mocks;

namespace Test;

[TestClass]
public class ListCommandsTest
{
    private MockRedisClient _mock = null!;
    private ListCommands _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _mock = new MockRedisClient();
        _sut = new ListCommands(_mock);
    }

    [TestMethod]
    public async Task LPushAsync_ReturnsListLength()
    {
        _mock.EnqueueInteger(3);
        var result = await _sut.LPushAsync("list", "c", "b", "a");
        Assert.AreEqual(3L, result);
        Assert.IsTrue(_mock.SentCommands[0].Contains("LPUSH"));
    }

    [TestMethod]
    public async Task RPushAsync_ReturnsListLength()
    {
        _mock.EnqueueInteger(2);
        var result = await _sut.RPushAsync("list", "a", "b");
        Assert.AreEqual(2L, result);
    }

    [TestMethod]
    public async Task LPopAsync_ReturnsElement()
    {
        _mock.EnqueueBulkString("a");
        var result = await _sut.LPopAsync("list");
        Assert.AreEqual("a", result);
    }

    [TestMethod]
    public async Task LPopAsync_EmptyList_ReturnsNull()
    {
        _mock.EnqueueResult(RedisResult.Null());
        var result = await _sut.LPopAsync("empty");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task LLenAsync_ReturnsLength()
    {
        _mock.EnqueueInteger(5);
        var result = await _sut.LLenAsync("list");
        Assert.AreEqual(5L, result);
    }

    [TestMethod]
    public async Task LRangeAsync_ReturnsElements()
    {
        _mock.EnqueueArray(new[]
        {
            RedisResult.BulkString("a"),
            RedisResult.BulkString("b"),
            RedisResult.BulkString("c")
        });
        var result = await _sut.LRangeAsync("list", 0, -1);
        Assert.AreEqual(3, result.Length);
        Assert.AreEqual("a", result[0]);
        Assert.AreEqual("c", result[2]);
    }

    [TestMethod]
    public async Task LRemAsync_ReturnsRemovedCount()
    {
        _mock.EnqueueInteger(2);
        var result = await _sut.LRemAsync("list", 2, "value");
        Assert.AreEqual(2L, result);
    }

    [TestMethod]
    public async Task LIndexAsync_ReturnsElement()
    {
        _mock.EnqueueBulkString("b");
        var result = await _sut.LIndexAsync("list", 1);
        Assert.AreEqual("b", result);
    }

    [TestMethod]
    public async Task LSetAsync_SendsCorrectCommand()
    {
        _mock.EnqueueOK();
        var result = await _sut.LSetAsync("list", 0, "new");
        Assert.AreEqual("OK", result);
    }

    [TestMethod]
    public async Task LTrimAsync_ReturnsOK()
    {
        _mock.EnqueueOK();
        var result = await _sut.LTrimAsync("list", 0, 10);
        Assert.AreEqual("OK", result);
    }
}
