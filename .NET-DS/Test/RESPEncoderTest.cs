using SimpleRedis;

namespace Test;

[TestClass]
public class RESPEncoderTest
{
    [TestMethod]
    public void EncodeArgs_SingleArg_ReturnsRESPArray()
    {
        var result = RESPEncoder.EncodeArgs("PING");
        Assert.AreEqual("*1\r\n$4\r\nPING\r\n", result);
    }

    [TestMethod]
    public void EncodeArgs_MultipleArgs_ReturnsRESPArray()
    {
        var result = RESPEncoder.EncodeArgs("SET", "name", "Tom");
        Assert.AreEqual("*3\r\n$3\r\nSET\r\n$4\r\nname\r\n$3\r\nTom\r\n", result);
    }

    [TestMethod]
    public void EncodeArgs_WithEmptyString_IncludesEmptyBulkString()
    {
        var result = RESPEncoder.EncodeArgs("SET", "key", "");
        Assert.AreEqual("*3\r\n$3\r\nSET\r\n$3\r\nkey\r\n$0\r\n\r\n", result);
    }

    [TestMethod]
    public void Encode_WithSpaces_ReturnsCorrectRESP()
    {
        var result = RESPEncoder.Encode("GET mykey");
        Assert.AreEqual("*2\r\n$3\r\nGET\r\n$5\r\nmykey\r\n", result);
    }

    [TestMethod]
    public void Encode_WithExtraSpaces_HandlesCorrectly()
    {
        var result = RESPEncoder.Encode("  GET   mykey  ");
        Assert.AreEqual("*2\r\n$3\r\nGET\r\n$5\r\nmykey\r\n", result);
    }

    [TestMethod]
    public void Encode_ChinesCharacters_CountsBytesNotChars()
    {
        var result = RESPEncoder.EncodeArgs("SET", "name", "中文");
        // "中文" 在 UTF-8 中占 6 字节
        Assert.AreEqual("*3\r\n$3\r\nSET\r\n$4\r\nname\r\n$6\r\n中文\r\n", result);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Encode_EmptyCommand_Throws(string command)
    {
        RESPEncoder.Encode(command);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void EncodeArgs_NullArray_Throws()
    {
        RESPEncoder.EncodeArgs(null!);
    }

    [TestMethod]
    public void EncodeInteger_Positive_ReturnsCorrectFormat()
    {
        Assert.AreEqual(":42\r\n", RESPEncoder.EncodeInteger(42));
    }

    [TestMethod]
    public void EncodeInteger_Negative_ReturnsCorrectFormat()
    {
        Assert.AreEqual(":-1\r\n", RESPEncoder.EncodeInteger(-1));
    }

    [TestMethod]
    public void EncodeNull_ReturnsCorrectFormat()
    {
        Assert.AreEqual("$-1\r\n", RESPEncoder.EncodeNull());
    }

    [TestMethod]
    public void EncodeArgs_SpecialCharacters_EncodesCorrectly()
    {
        // 包含 \r\n 的值
        var result = RESPEncoder.EncodeArgs("SET", "key", "hello\r\nworld");
        Assert.AreEqual("*3\r\n$3\r\nSET\r\n$3\r\nkey\r\n$12\r\nhello\r\nworld\r\n", result);
        // $12 之后是 12 字节的内容 "hello\r\nworld"（\r\n 算 2 字节）
    }

    [TestMethod]
    public void EncodeArgs_GetCommand_ReturnsCorrectFormat()
    {
        var result = RESPEncoder.EncodeArgs("GET", "mykey");
        Assert.AreEqual("*2\r\n$3\r\nGET\r\n$5\r\nmykey\r\n", result);
    }
}
