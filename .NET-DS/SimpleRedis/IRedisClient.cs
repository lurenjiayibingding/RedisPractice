namespace SimpleRedis;

/// <summary>
/// Redis 客户端接口，用于解耦命令层与连接层，便于单元测试
/// </summary>
public interface IRedisClient
{
    /// <summary>发送已编码的 RESP 命令，返回解码后的结果</summary>
    Task<RedisResult> SendCommandAsync(string respCommand);

    /// <summary>建立连接并完成认证</summary>
    Task ConnectAsync();

    /// <summary>获取或设置当前数据库编号</summary>
    int Db { get; set; }
}
