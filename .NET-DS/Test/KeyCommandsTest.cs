using SimpleRedis;
using SimpleRedis.Commands;
using Test.Mocks;

namespace Test;

[TestClass]
public class KeyCommandsTest
{
    private MockRedisClient _mock = null!;
    private KeyCommands _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _mock = new MockRedisClient();
        _sut = new KeyCommands(_mock);
    }

    [TestMethod]
    public async Task DelAsync_ReturnsDeletedCount()
    {
        _mock.EnqueueInteger(2);
        var result = await _sut.DelAsync("k1", "k2");
        Assert.AreEqual(2L, result);
    }

    [TestMethod]
    public async Task ExistsAsync_ReturnsCount()
    {
        _mock.EnqueueInteger(3);
        var result = await _sut.ExistsAsync("k1", "k2", "k3");
        Assert.AreEqual(3L, result);
    }

    [TestMethod]
    public async Task ExpireAsync_ReturnsTrue()
    {
        _mock.EnqueueInteger(1);
        var result = await _sut.ExpireAsync("key", 60);
        Assert.IsTrue(result);
        Assert.IsTrue(_mock.SentCommands[0].Contains("EXPIRE"));
    }

    [TestMethod]
    public async Task ExpireAsync_KeyNotExist_ReturnsFalse()
    {
        _mock.EnqueueInteger(0);
        var result = await _sut.ExpireAsync("nonexistent", 60);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task TTLAsync_ReturnsSeconds()
    {
        _mock.EnqueueInteger(59);
        var result = await _sut.TTLAsync("key");
        Assert.AreEqual(59L, result);
    }

    [TestMethod]
    public async Task TTLAsync_NoExpiry_ReturnsMinusOne()
    {
        _mock.EnqueueInteger(-1);
        var result = await _sut.TTLAsync("persistent-key");
        Assert.AreEqual(-1L, result);
    }

    [TestMethod]
    public async Task PersistAsync_ReturnsTrue()
    {
        _mock.EnqueueInteger(1);
        var result = await _sut.PersistAsync("key");
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task TypeAsync_ReturnsType()
    {
        _mock.EnqueueResult(RedisResult.SimpleString("string"));
        var result = await _sut.TypeAsync("key");
        Assert.AreEqual("string", result);
    }

    [TestMethod]
    public async Task RenameAsync_SendsCorrectCommand()
    {
        _mock.EnqueueOK();
        await _sut.RenameAsync("old", "new");
        Assert.IsTrue(_mock.SentCommands[0].Contains("RENAME"));
        Assert.IsTrue(_mock.SentCommands[0].Contains("old"));
        Assert.IsTrue(_mock.SentCommands[0].Contains("new"));
    }

    [TestMethod]
    public async Task ScanAsync_ReturnsCursorAndKeys()
    {
        _mock.EnqueueArray(new[]
        {
            RedisResult.BulkString("5"),
            RedisResult.Array(new[] { RedisResult.BulkString("k1"), RedisResult.BulkString("k2") })
        });
        var (cursor, keys) = await _sut.ScanAsync(0);
        Assert.AreEqual(5L, cursor);
        Assert.AreEqual(2, keys.Length);
        Assert.AreEqual("k1", keys[0]);
    }

    [TestMethod]
    public async Task ScanAsync_WithMatch_IncludesPattern()
    {
        _mock.EnqueueArray(new[]
        {
            RedisResult.BulkString("0"),
            RedisResult.Array(new[] { RedisResult.BulkString("k1") })
        });
        await _sut.ScanAsync(0, "k*", 10);
        Assert.IsTrue(_mock.SentCommands[0].Contains("MATCH"));
        Assert.IsTrue(_mock.SentCommands[0].Contains("k*"));
        Assert.IsTrue(_mock.SentCommands[0].Contains("COUNT"));
        Assert.IsTrue(_mock.SentCommands[0].Contains("10"));
    }
}
