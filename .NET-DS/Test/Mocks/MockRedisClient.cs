using SimpleRedis;

namespace Test.Mocks;

/// <summary>
/// 用于单元测试的模拟 Redis 客户端
/// 记录发送的命令并返回预设的响应
/// </summary>
public class MockRedisClient : IRedisClient
{
    private readonly Queue<RedisResult> _results = new();
    private int _db;

    /// <summary>所有已发送的 RESP 命令</summary>
    public List<string> SentCommands { get; } = new();

    /// <summary>预设响应队列（按先进先出顺序返回）</summary>
    public void EnqueueResult(RedisResult result) => _results.Enqueue(result);

    /// <summary>预设一个 OK 响应</summary>
    public void EnqueueOK() => EnqueueResult(RedisResult.SimpleString("OK"));

    /// <summary>预设一个整数响应</summary>
    public void EnqueueInteger(long value) => EnqueueResult(RedisResult.Integer(value));

    /// <summary>预设一个批量字符串响应</summary>
    public void EnqueueBulkString(string? value) => EnqueueResult(RedisResult.BulkString(value));

    /// <summary>预设一个数组响应</summary>
    public void EnqueueArray(RedisResult[] items) => EnqueueResult(RedisResult.Array(items));

    /// <summary>清空所有预设响应和发送记录</summary>
    public void Clear()
    {
        _results.Clear();
        SentCommands.Clear();
    }

    public Task<RedisResult> SendCommandAsync(string respCommand)
    {
        SentCommands.Add(respCommand);
        if (_results.Count == 0)
            return Task.FromResult(RedisResult.SimpleString("OK"));
        return Task.FromResult(_results.Dequeue());
    }

    public Task ConnectAsync()
    {
        _db = 0;
        return Task.CompletedTask;
    }

    public int Db
    {
        get => _db;
        set => _db = value;
    }
}
