using SimpleRedis;
using SimpleRedis.Enum;
using SimpleRedis.Exceptions;

namespace Test;

[TestClass]
public class RedisResultTest
{
    // ---- 工厂方法 ----

    [TestMethod]
    public void SimpleString_CreatesCorrectType()
    {
        var r = RedisResult.SimpleString("OK");
        Assert.AreEqual(ResultTypeEnum.SimpleString, r.Type);
        Assert.AreEqual("OK", r.AsString());
    }

    [TestMethod]
    public void BulkString_CreatesCorrectType()
    {
        var r = RedisResult.BulkString("hello");
        Assert.AreEqual(ResultTypeEnum.BulkString, r.Type);
        Assert.AreEqual("hello", r.AsString());
    }

    [TestMethod]
    public void NullBulkString_IsNull()
    {
        var r = RedisResult.BulkString(null);
        Assert.AreEqual(ResultTypeEnum.BulkString, r.Type);
        Assert.IsNull(r.AsString());
    }

    [TestMethod]
    public void Integer_CreatesCorrectType()
    {
        var r = RedisResult.Integer(100);
        Assert.AreEqual(ResultTypeEnum.Integer, r.Type);
        Assert.AreEqual(100L, r.AsInt64());
    }

    [TestMethod]
    public void NullResult_IsNull()
    {
        var r = RedisResult.Null();
        Assert.AreEqual(ResultTypeEnum.Null, r.Type);
        Assert.IsTrue(r.IsNull());
    }

    [TestMethod]
    public void NullArray_ArrayLengthZero()
    {
        var r = RedisResult.NullArray();
        Assert.AreEqual(ResultTypeEnum.Array, r.Type);
        Assert.AreEqual(0, r.AsArray().Length);
    }

    // ---- 类型转换 ----

    [TestMethod]
    public void AsString_IntegerResult_ReturnsString()
    {
        var r = RedisResult.Integer(42);
        Assert.AreEqual("42", r.AsString());
    }

    [TestMethod]
    public void AsInt64_FromBulkString_Converts()
    {
        var r = RedisResult.BulkString("100");
        Assert.AreEqual(100L, r.AsInt64());
    }

    [TestMethod]
    public void AsBoolean_FromBooleanTrue_ReturnsTrue()
    {
        var r = RedisResult.Boolean(true);
        Assert.IsTrue(r.AsBoolean());
    }

    [TestMethod]
    public void AsBoolean_FromIntegerOne_ReturnsTrue()
    {
        var r = RedisResult.Integer(1);
        Assert.IsTrue(r.AsBoolean());
    }

    [TestMethod]
    public void AsDouble_FromInteger_Converts()
    {
        var r = RedisResult.Integer(5);
        Assert.AreEqual(5.0, r.AsDouble(), 0.001);
    }

    [TestMethod]
    public void AsDouble_FromDouble_ReturnsValue()
    {
        var r = RedisResult.Double(3.14);
        Assert.AreEqual(3.14, r.AsDouble(), 0.001);
    }

    // ---- 错误处理 ----

    [TestMethod]
    [ExpectedException(typeof(RedisServerException))]
    public void Error_ThrowIfError_Throws()
    {
        var r = RedisResult.Error("ERR something");
        r.ThrowIfError();
    }

    [TestMethod]
    public void Error_AsString_Throws()
    {
        var r = RedisResult.Error("ERR test");
        Assert.ThrowsException<RedisServerException>(() => r.AsString());
    }

    [TestMethod]
    [ExpectedException(typeof(RedisProtocolException))]
    public void AsArray_OnInteger_Throws()
    {
        var r = RedisResult.Integer(42);
        r.AsArray();
    }

    // ---- ToString ----

    [TestMethod]
    public void ToString_Null_ReturnsNull()
    {
        var r = RedisResult.Null();
        Assert.AreEqual("(null)", r.ToString());
    }

    [TestMethod]
    public void ToString_SimpleString_ReturnsValue()
    {
        var r = RedisResult.SimpleString("OK");
        Assert.AreEqual("OK", r.ToString());
    }

    // ---- Equals ----

    [TestMethod]
    public void Equals_SameTypeAndValue_ReturnsTrue()
    {
        var r1 = RedisResult.Integer(42);
        var r2 = RedisResult.Integer(42);
        Assert.AreEqual(r1, r2);
    }

    [TestMethod]
    public void Equals_DifferentType_ReturnsFalse()
    {
        var r1 = RedisResult.Integer(42);
        var r2 = RedisResult.BulkString("42");
        Assert.AreNotEqual(r1, r2);
    }

    [TestMethod]
    public void Equals_NullResults_AreEqual()
    {
        var r1 = RedisResult.Null();
        var r2 = RedisResult.Null();
        Assert.AreEqual(r1, r2);
    }
}
