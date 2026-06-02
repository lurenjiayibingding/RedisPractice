using SimpleRedis.Enum;
using SimpleRedis.Exceptions;

namespace SimpleRedis;

/// <summary>
/// Redis 响应结果封装，提供类型安全的访问方法
/// </summary>
public class RedisResult
{
    public ResultTypeEnum Type { get; }
    public object? RawValue { get; }
    public string? ErrorMessage { get; }

    private RedisResult(ResultTypeEnum type, object? rawValue, string? errorMessage = null)
    {
        Type = type;
        RawValue = rawValue;
        ErrorMessage = errorMessage;
    }

    // ---- 工厂方法 ----

    public static RedisResult SimpleString(string value) => new(ResultTypeEnum.SimpleString, value);
    public static RedisResult Error(string message) => new(ResultTypeEnum.Error, null, message);
    public static RedisResult Integer(long value) => new(ResultTypeEnum.Integer, value);
    public static RedisResult BulkString(string? value) => new(ResultTypeEnum.BulkString, value);
    public static RedisResult Array(RedisResult[] items) => new(ResultTypeEnum.Array, items);
    public static RedisResult NullArray() => new(ResultTypeEnum.Array, null);
    public static RedisResult Null() => new(ResultTypeEnum.Null, null);
    public static RedisResult Boolean(bool value) => new(ResultTypeEnum.Boolean, value);
    public static RedisResult Double(double value) => new(ResultTypeEnum.Double, value);
    public static RedisResult Map(Dictionary<RedisResult, RedisResult> dict) => new(ResultTypeEnum.Map, dict);
    public static RedisResult Set(RedisResult[] items) => new(ResultTypeEnum.Set, items);
    public static RedisResult Push(RedisResult[] items) => new(ResultTypeEnum.Push, items);

    // ---- 类型转换方法 ----

    /// <summary>
    /// 如果响应为错误类型则抛出异常
    /// </summary>
    public void ThrowIfError()
    {
        if (Type == ResultTypeEnum.Error)
            throw new RedisServerException(ErrorMessage ?? "未知服务器错误");
    }

    /// <summary>
    /// 转为字符串（支持 SimpleString, BulkString, Integer）
    /// </summary>
    public string? AsString()
    {
        ThrowIfError();
        return Type switch
        {
            ResultTypeEnum.SimpleString => (string)RawValue!,
            ResultTypeEnum.BulkString => (string?)RawValue,
            ResultTypeEnum.Integer => ((long)RawValue!).ToString(),
            _ => throw new RedisProtocolException($"无法将 {Type} 转换为字符串")
        };
    }

    /// <summary>
    /// 转为整数
    /// </summary>
    public long AsInt64()
    {
        ThrowIfError();
        return Type switch
        {
            ResultTypeEnum.Integer => (long)RawValue!,
            ResultTypeEnum.SimpleString or ResultTypeEnum.BulkString => long.Parse((string)RawValue!),
            _ => throw new RedisProtocolException($"无法将 {Type} 转换为整数")
        };
    }

    /// <summary>
    /// 转为浮点数
    /// </summary>
    public double AsDouble()
    {
        ThrowIfError();
        return Type switch
        {
            ResultTypeEnum.Double => (double)RawValue!,
            ResultTypeEnum.Integer => Convert.ToDouble((long)RawValue!),
            ResultTypeEnum.SimpleString or ResultTypeEnum.BulkString => double.Parse((string)RawValue!),
            _ => throw new RedisProtocolException($"无法将 {Type} 转换为浮点数")
        };
    }

    /// <summary>
    /// 转为布尔值
    /// </summary>
    public bool AsBoolean()
    {
        ThrowIfError();
        return Type switch
        {
            ResultTypeEnum.Boolean => (bool)RawValue!,
            ResultTypeEnum.Integer => (long)RawValue! != 0,
            ResultTypeEnum.SimpleString or ResultTypeEnum.BulkString => string.Equals((string)RawValue!, "OK", StringComparison.OrdinalIgnoreCase) || (string)RawValue! == "1",
            _ => throw new RedisProtocolException($"无法将 {Type} 转换为布尔值")
        };
    }

    /// <summary>
    /// 转为结果数组
    /// </summary>
    public RedisResult[] AsArray()
    {
        ThrowIfError();
        return Type switch
        {
            ResultTypeEnum.Array => (RedisResult[]?)RawValue ?? System.Array.Empty<RedisResult>(),
            ResultTypeEnum.Set or ResultTypeEnum.Push => (RedisResult[])RawValue!,
            _ => throw new RedisProtocolException($"无法将 {Type} 转换为数组")
        };
    }

    /// <summary>
    /// 转为字典
    /// </summary>
    public Dictionary<RedisResult, RedisResult> AsMap()
    {
        ThrowIfError();
        return Type switch
        {
            ResultTypeEnum.Map => (Dictionary<RedisResult, RedisResult>)RawValue!,
            _ => throw new RedisProtocolException($"无法将 {Type} 转换为字典")
        };
    }

    /// <summary>
    /// 判断是否为 null
    /// </summary>
    public bool IsNull() => Type == ResultTypeEnum.Null;

    public override string ToString()
    {
        return Type switch
        {
            ResultTypeEnum.Null => "(null)",
            ResultTypeEnum.Error => $"Error: {ErrorMessage}",
            ResultTypeEnum.Array => $"Array[{((RedisResult[]?)RawValue)?.Length ?? 0}]",
            ResultTypeEnum.Map => $"Map[{(RawValue as System.Collections.IDictionary)?.Count ?? 0}]",
            ResultTypeEnum.Set => $"Set[{((RedisResult[]?)RawValue)?.Length ?? 0}]",
            ResultTypeEnum.Push => $"Push[{((RedisResult[]?)RawValue)?.Length ?? 0}]",
            _ => RawValue?.ToString() ?? "(null)"
        };
    }

    public override bool Equals(object? obj)
    {
        if (obj is not RedisResult other) return false;
        if (Type != other.Type) return false;

        return (Type, RawValue, other.RawValue) switch
        {
            (ResultTypeEnum.Null, _, _) => true,
            (ResultTypeEnum.Error, _, _) => ErrorMessage == other.ErrorMessage,
            _ => Equals(RawValue, other.RawValue)
        };
    }

    public override int GetHashCode() => HashCode.Combine(Type, RawValue);
}
