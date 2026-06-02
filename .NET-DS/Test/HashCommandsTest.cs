using SimpleRedis;
using SimpleRedis.Commands;
using Test.Mocks;

namespace Test;

[TestClass]
public class HashCommandsTest
{
    private MockRedisClient _mock = null!;
    private HashCommands _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _mock = new MockRedisClient();
        _sut = new HashCommands(_mock);
    }

    [TestMethod]
    public async Task HSetAsync_NewField_ReturnsTrue()
    {
        _mock.EnqueueInteger(1);
        var result = await _sut.HSetAsync("user:1", "name", "Tom");
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task HSetAsync_ExistingField_ReturnsFalse()
    {
        _mock.EnqueueInteger(0);
        var result = await _sut.HSetAsync("user:1", "name", "Tom");
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task HGetAsync_ReturnsValue()
    {
        _mock.EnqueueBulkString("Tom");
        var result = await _sut.HGetAsync("user:1", "name");
        Assert.AreEqual("Tom", result);
    }

    [TestMethod]
    public async Task HGetAsync_FieldNotExist_ReturnsNull()
    {
        _mock.EnqueueResult(RedisResult.Null());
        var result = await _sut.HGetAsync("user:1", "nonexistent");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task HGetAllAsync_ReturnsDict()
    {
        _mock.EnqueueArray(new[]
        {
            RedisResult.BulkString("name"),
            RedisResult.BulkString("Tom"),
            RedisResult.BulkString("age"),
            RedisResult.BulkString("30")
        });
        var result = await _sut.HGetAllAsync("user:1");
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("Tom", result["name"]);
        Assert.AreEqual("30", result["age"]);
    }

    [TestMethod]
    public async Task HGetAllAsync_Empty_ReturnsEmptyDict()
    {
        _mock.EnqueueArray(Array.Empty<RedisResult>());
        var result = await _sut.HGetAllAsync("empty");
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task HDelAsync_RemovesFields()
    {
        _mock.EnqueueInteger(2);
        var result = await _sut.HDelAsync("user:1", "name", "age");
        Assert.AreEqual(2L, result);
    }

    [TestMethod]
    public async Task HExistsAsync_FieldExists_ReturnsTrue()
    {
        _mock.EnqueueInteger(1);
        var result = await _sut.HExistsAsync("user:1", "name");
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task HKeysAsync_ReturnsFields()
    {
        _mock.EnqueueArray(new[]
        {
            RedisResult.BulkString("name"),
            RedisResult.BulkString("age")
        });
        var result = await _sut.HKeysAsync("user:1");
        CollectionAssert.AreEqual(new[] { "name", "age" }, result);
    }

    [TestMethod]
    public async Task HValsAsync_ReturnsValues()
    {
        _mock.EnqueueArray(new[]
        {
            RedisResult.BulkString("Tom"),
            RedisResult.BulkString("30")
        });
        var result = await _sut.HValsAsync("user:1");
        CollectionAssert.AreEqual(new[] { "Tom", "30" }, result);
    }

    [TestMethod]
    public async Task HLenAsync_ReturnsCount()
    {
        _mock.EnqueueInteger(2);
        var result = await _sut.HLenAsync("user:1");
        Assert.AreEqual(2L, result);
    }

    [TestMethod]
    public async Task HIncrByAsync_ReturnsNewValue()
    {
        _mock.EnqueueInteger(31);
        var result = await _sut.HIncrByAsync("user:1", "age", 1);
        Assert.AreEqual(31L, result);
    }

    [TestMethod]
    public async Task HMGetAsync_ReturnsValues()
    {
        _mock.EnqueueArray(new[]
        {
            RedisResult.BulkString("Tom"),
            RedisResult.Null()
        });
        var result = await _sut.HMGetAsync("user:1", "name", "nonexistent");
        Assert.AreEqual("Tom", result[0]);
        Assert.IsNull(result[1]);
    }
}
