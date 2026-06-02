using SimpleRedis.Enum;
using SimpleRedis.Exceptions;
using System.Text;

namespace SimpleRedis;

/// <summary>
/// RESP 协议解码器
/// 将 Redis 服务器返回的字节数据解析为 RedisResult
/// </summary>
public static class RESPDecoder
{
    /// <summary>
    /// 解码完整的 RESP 响应数据
    /// </summary>
    public static RedisResult Decode(byte[] data)
    {
        if (data == null || data.Length == 0)
            throw new RedisProtocolException("响应数据为空");

        var position = 0;
        return DecodeInternal(data, ref position);
    }

    private static RedisResult DecodeInternal(byte[] data, ref int position)
    {
        if (position >= data.Length)
            throw new RedisProtocolException("意外的数据结尾");

        var firstByte = data[position++];
        return firstByte switch
        {
            (byte)'+' => DecodeSimpleString(data, ref position),
            (byte)'-' => DecodeError(data, ref position),
            (byte)':' => DecodeInteger(data, ref position),
            (byte)'$' => DecodeBulkString(data, ref position),
            (byte)'*' => DecodeArray(data, ref position),
            (byte)'%' => DecodeMap(data, ref position),
            (byte)'~' => DecodeCollection(data, ref position, ResultTypeEnum.Set),
            (byte)'#' => DecodeBoolean(data, ref position),
            (byte)'_' => DecodeNull(data, ref position),
            (byte)',' => DecodeDouble(data, ref position),
            (byte)'>' => DecodePush(data, ref position),
            _ => throw new RedisProtocolException($"未知的 RESP 类型标识: {(char)firstByte}")
        };
    }

    private static RedisResult DecodeSimpleString(byte[] data, ref int position)
    {
        var line = ReadLine(data, ref position);
        return RedisResult.SimpleString(line);
    }

    private static RedisResult DecodeError(byte[] data, ref int position)
    {
        var line = ReadLine(data, ref position);
        return RedisResult.Error(line);
    }

    private static RedisResult DecodeInteger(byte[] data, ref int position)
    {
        var line = ReadLine(data, ref position);
        if (!long.TryParse(line, out var value))
            throw new RedisProtocolException($"无效的整数响应: {line}");
        return RedisResult.Integer(value);
    }

    private static RedisResult DecodeBulkString(byte[] data, ref int position)
    {
        var line = ReadLine(data, ref position);

        // $-1 表示 null
        if (line == "-1")
            return RedisResult.Null();

        if (!int.TryParse(line, out var length) || length < 0)
            throw new RedisProtocolException($"无效的批量字符串长度: {line}");

        if (position + length > data.Length)
            throw new RedisProtocolException("批量字符串数据不足");

        var str = Encoding.UTF8.GetString(data, position, length);
        position += length;

        // 跳过尾部的 \r\n
        if (position + 1 < data.Length && data[position] == '\r' && data[position + 1] == '\n')
            position += 2;

        return RedisResult.BulkString(str);
    }

    private static RedisResult DecodeArray(byte[] data, ref int position)
    {
        var line = ReadLine(data, ref position);

        // *-1 表示 null 数组
        if (line == "-1")
            return RedisResult.NullArray();

        if (!int.TryParse(line, out var count) || count < 0)
            throw new RedisProtocolException($"无效的数组长度: {line}");

        var items = new RedisResult[count];
        for (var i = 0; i < count; i++)
        {
            items[i] = DecodeInternal(data, ref position);
        }

        return RedisResult.Array(items);
    }

    private static RedisResult DecodeMap(byte[] data, ref int position)
    {
        var line = ReadLine(data, ref position);

        if (!int.TryParse(line, out var count) || count < 0)
            throw new RedisProtocolException($"无效的 Map 长度: {line}");

        var dict = new Dictionary<RedisResult, RedisResult>(count);
        for (var i = 0; i < count; i++)
        {
            var key = DecodeInternal(data, ref position);
            var value = DecodeInternal(data, ref position);
            dict[key] = value;
        }

        return RedisResult.Map(dict);
    }

    private static RedisResult DecodeCollection(byte[] data, ref int position, ResultTypeEnum type)
    {
        var line = ReadLine(data, ref position);

        if (!int.TryParse(line, out var count) || count < 0)
            throw new RedisProtocolException($"无效的集合长度: {line}");

        var items = new RedisResult[count];
        for (var i = 0; i < count; i++)
        {
            items[i] = DecodeInternal(data, ref position);
        }

        return type == ResultTypeEnum.Set ? RedisResult.Set(items) : RedisResult.Push(items);
    }

    private static RedisResult DecodeBoolean(byte[] data, ref int position)
    {
        var line = ReadLine(data, ref position);
        return line switch
        {
            "t" => RedisResult.Boolean(true),
            "f" => RedisResult.Boolean(false),
            _ => throw new RedisProtocolException($"无效的布尔值: {line}")
        };
    }

    private static RedisResult DecodeNull(byte[] data, ref int position)
    {
        // 跳过 \r\n
        if (position + 1 < data.Length && data[position] == '\r' && data[position + 1] == '\n')
            position += 2;
        return RedisResult.Null();
    }

    private static RedisResult DecodeDouble(byte[] data, ref int position)
    {
        var line = ReadLine(data, ref position);
        if (!double.TryParse(line, out var value))
            throw new RedisProtocolException($"无效的浮点数: {line}");
        return RedisResult.Double(value);
    }

    private static RedisResult DecodePush(byte[] data, ref int position)
    {
        return DecodeCollection(data, ref position, ResultTypeEnum.Push);
    }

    /// <summary>
    /// 读取直到 \r\n，返回中间的字符串（不包含 \r\n）
    /// </summary>
    private static string ReadLine(byte[] data, ref int position)
    {
        var start = position;
        while (position < data.Length)
        {
            if (data[position] == '\r' && position + 1 < data.Length && data[position + 1] == '\n')
            {
                var line = Encoding.UTF8.GetString(data, start, position - start);
                position += 2; // 跳过 \r\n
                return line;
            }
            position++;
        }

        throw new RedisProtocolException("未找到 \\r\\n 结束符");
    }
}
