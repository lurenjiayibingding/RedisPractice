using SimpleRedis;
using SimpleRedis.Enum;
using SimpleRedis.Exceptions;
using System.Text;

namespace Test;

[TestClass]
public class RESPDecoderTest
{
    // ---- 简单字符串 ----

    [TestMethod]
    public void Decode_SimpleString_ReturnsString()
    {
        var data = Encoding.UTF8.GetBytes("+OK\r\n");
        var result = RESPDecoder.Decode(data);
        Assert.AreEqual(ResultTypeEnum.SimpleString, result.Type);
        Assert.AreEqual("OK", result.AsString());
    }

    [TestMethod]
    public void Decode_SimpleString_WithText_ReturnsText()
    {
        var data = Encoding.UTF8.GetBytes("+HELLO\r\n");
        var result = RESPDecoder.Decode(data);
        Assert.AreEqual("HELLO", result.AsString());
    }

    // ---- 错误 ----

    [TestMethod]
    public void Decode_Error_ReturnsErrorResult()
    {
        var data = Encoding.UTF8.GetBytes("-ERR unknown command\r\n");
        var result = RESPDecoder.Decode(data);
        Assert.AreEqual(ResultTypeEnum.Error, result.Type);
        Assert.AreEqual("ERR unknown command", result.ErrorMessage);
        Assert.ThrowsException<RedisServerException>(() => result.AsString());
    }

    // ---- 整数 ----

    [TestMethod]
    public void Decode_Integer_Positive_ReturnsLong()
    {
        var data = Encoding.UTF8.GetBytes(":42\r\n");
        var result = RESPDecoder.Decode(data);
        Assert.AreEqual(ResultTypeEnum.Integer, result.Type);
        Assert.AreEqual(42L, result.AsInt64());
    }

    [TestMethod]
    public void Decode_Integer_Negative_ReturnsLong()
    {
        var data = Encoding.UTF8.GetBytes(":-1\r\n");
        var result = RESPDecoder.Decode(data);
        Assert.AreEqual(-1L, result.AsInt64());
    }

    // ---- 批量字符串 ----

    [TestMethod]
    public void Decode_BulkString_ReturnsString()
    {
        var data = Encoding.UTF8.GetBytes("$5\r\nhello\r\n");
        var result = RESPDecoder.Decode(data);
        Assert.AreEqual(ResultTypeEnum.BulkString, result.Type);
        Assert.AreEqual("hello", result.AsString());
    }

    [TestMethod]
    public void Decode_BulkString_Null_ReturnsNull()
    {
        var data = Encoding.UTF8.GetBytes("$-1\r\n");
        var result = RESPDecoder.Decode(data);
        Assert.AreEqual(ResultTypeEnum.Null, result.Type);
        Assert.IsTrue(result.IsNull());
    }

    [TestMethod]
    public void Decode_BulkString_Empty_ReturnsEmptyString()
    {
        var data = Encoding.UTF8.GetBytes("$0\r\n\r\n");
        var result = RESPDecoder.Decode(data);
        Assert.AreEqual(ResultTypeEnum.BulkString, result.Type);
        Assert.AreEqual("", result.AsString());
    }

    [TestMethod]
    public void Decode_BulkString_Chinese_ReturnsString()
    {
        var data = Encoding.UTF8.GetBytes("$6\r\n中文\r\n");
        var result = RESPDecoder.Decode(data);
        Assert.AreEqual("中文", result.AsString());
    }

    // ---- 数组 ----

    [TestMethod]
    public void Decode_Array_TwoStrings_ReturnsArray()
    {
        var data = Encoding.UTF8.GetBytes("*2\r\n$3\r\nGET\r\n$4\r\nname\r\n");
        var result = RESPDecoder.Decode(data);
        Assert.AreEqual(ResultTypeEnum.Array, result.Type);
        var arr = result.AsArray();
        Assert.AreEqual(2, arr.Length);
        Assert.AreEqual("GET", arr[0].AsString());
        Assert.AreEqual("name", arr[1].AsString());
    }

    [TestMethod]
    public void Decode_Array_Null_ReturnsNullArray()
    {
        var data = Encoding.UTF8.GetBytes("*-1\r\n");
        var result = RESPDecoder.Decode(data);
        Assert.AreEqual(ResultTypeEnum.Array, result.Type);
        Assert.AreEqual(0, result.AsArray().Length);
    }

    [TestMethod]
    public void Decode_Array_Empty_ReturnsEmptyArray()
    {
        var data = Encoding.UTF8.GetBytes("*0\r\n");
        var result = RESPDecoder.Decode(data);
        Assert.AreEqual(0, result.AsArray().Length);
    }

    [TestMethod]
    public void Decode_Array_NestedArray_ReturnsNested()
    {
        // *2\r\n*1\r\n$3\r\nGET\r\n$4\r\nname\r\n
        var data = Encoding.UTF8.GetBytes("*2\r\n*1\r\n$3\r\nGET\r\n$4\r\nname\r\n");
        var result = RESPDecoder.Decode(data);
        var arr = result.AsArray();
        Assert.AreEqual(2, arr.Length);
        Assert.AreEqual(ResultTypeEnum.Array, arr[0].Type);
        Assert.AreEqual("name", arr[1].AsString());
    }

    // ---- 布尔值 ----

    [TestMethod]
    public void Decode_Boolean_True_ReturnsTrue()
    {
        var data = Encoding.UTF8.GetBytes("#t\r\n");
        var result = RESPDecoder.Decode(data);
        Assert.AreEqual(ResultTypeEnum.Boolean, result.Type);
        Assert.IsTrue(result.AsBoolean());
    }

    [TestMethod]
    public void Decode_Boolean_False_ReturnsFalse()
    {
        var data = Encoding.UTF8.GetBytes("#f\r\n");
        var result = RESPDecoder.Decode(data);
        Assert.AreEqual(ResultTypeEnum.Boolean, result.Type);
        Assert.IsFalse(result.AsBoolean());
    }

    // ---- Null ----

    [TestMethod]
    public void Decode_Null_ReturnsNull()
    {
        var data = Encoding.UTF8.GetBytes("_\r\n");
        var result = RESPDecoder.Decode(data);
        Assert.AreEqual(ResultTypeEnum.Null, result.Type);
        Assert.IsTrue(result.IsNull());
    }

    // ---- 浮点数 ----

    [TestMethod]
    public void Decode_Double_ReturnsDouble()
    {
        var data = Encoding.UTF8.GetBytes(",3.14\r\n");
        var result = RESPDecoder.Decode(data);
        Assert.AreEqual(ResultTypeEnum.Double, result.Type);
        Assert.AreEqual(3.14, result.AsDouble(), 0.001);
    }

    // ---- Map (RESP3) ----

    [TestMethod]
    public void Decode_Map_ReturnsDict()
    {
        var data = Encoding.UTF8.GetBytes("%2\r\n+key1\r\n+value1\r\n+key2\r\n+value2\r\n");
        var result = RESPDecoder.Decode(data);
        Assert.AreEqual(ResultTypeEnum.Map, result.Type);
        var map = result.AsMap();
        Assert.AreEqual(2, map.Count);
    }

    // ---- 混合类型 ----

    [TestMethod]
    public void Decode_BulkString_Integer_ConvertsCorrectly()
    {
        // $ 开头的 "42" 应该能通过 AsInt64 转为 42
        var data = Encoding.UTF8.GetBytes("$2\r\n42\r\n");
        var result = RESPDecoder.Decode(data);
        Assert.AreEqual(42L, result.AsInt64());
    }

    // ---- 错误处理 ----

    [TestMethod]
    [ExpectedException(typeof(RedisProtocolException))]
    public void Decode_EmptyData_Throws()
    {
        RESPDecoder.Decode(Array.Empty<byte>());
    }

    [TestMethod]
    [ExpectedException(typeof(RedisProtocolException))]
    public void Decode_UnknownType_Throws()
    {
        var data = Encoding.UTF8.GetBytes("|unknown\r\n");
        RESPDecoder.Decode(data);
    }

    [TestMethod]
    [ExpectedException(typeof(RedisProtocolException))]
    public void Decode_TruncatedData_Throws()
    {
        // $5 但只有 3 字节数据
        var data = Encoding.UTF8.GetBytes("$5\r\nhel");
        RESPDecoder.Decode(data);
    }

    [TestMethod]
    public void Decode_Set_ReturnsArray()
    {
        var data = Encoding.UTF8.GetBytes("~2\r\n$3\r\none\r\n$3\r\ntwo\r\n");
        var result = RESPDecoder.Decode(data);
        Assert.AreEqual(ResultTypeEnum.Set, result.Type);
        var arr = result.AsArray();
        Assert.AreEqual(2, arr.Length);
        Assert.AreEqual("one", arr[0].AsString());
        Assert.AreEqual("two", arr[1].AsString());
    }
}
