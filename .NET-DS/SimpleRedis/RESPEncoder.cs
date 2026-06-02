using System.Text;

namespace SimpleRedis;

/// <summary>
/// RESP (REdis Serialization Protocol) 编码器
/// 将命令参数编码为 RESP 协议格式
/// </summary>
public static class RESPEncoder
{
    /// <summary>
    /// 将字符串数组编码为 RESP 数组格式
    /// </summary>
    public static string EncodeArgs(params string[] args)
    {
        if (args == null || args.Length == 0)
            throw new ArgumentNullException(nameof(args));

        var sb = new StringBuilder();
        foreach (var arg in args)
        {
            var byteCount = Encoding.UTF8.GetByteCount(arg);
            sb.Append('$').Append(byteCount).Append("\r\n");
            sb.Append(arg).Append("\r\n");
        }
        sb.Insert(0, $"*{args.Length}\r\n");
        return sb.ToString();
    }

    /// <summary>
    /// 将空格分隔的命令字符串编码为 RESP 格式
    /// </summary>
    public static string Encode(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentNullException(nameof(command));

        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return EncodeArgs(parts);
    }

    /// <summary>
    /// 编码 Null 批量字符串
    /// </summary>
    public static string EncodeNull() => "$-1\r\n";

    /// <summary>
    /// 编码整数
    /// </summary>
    public static string EncodeInteger(long value) => $":{value}\r\n";
}
