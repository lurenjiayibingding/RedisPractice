using SimpleRedis;
using SimpleRedis.Commands;

Console.WriteLine("=== SimpleRedis Console Demo ===\n");

// 创建客户端（连接池自动管理）
var client = RedisClient.CreateClient("127.0.0.1", 6379, "", "");

try
{
    // 连接并认证
    await client.ConnectAsync();
    Console.WriteLine("✓ 连接成功");

    // 创建命令对象
    var strings = new StringCommands(client);
    var server = new ServerCommands(client);
    var keys = new KeyCommands(client);

    // PING
    var ping = await server.PingAsync();
    Console.WriteLine($"✓ PING: {ping}");

    // SET / GET
    await strings.SetAsync("dotnet:hello", "Hello from .NET!");
    var val = await strings.GetAsync("dotnet:hello");
    Console.WriteLine($"✓ GET dotnet:hello = {val}");

    // INCR
    var count = await strings.IncrAsync("dotnet:counter");
    Console.WriteLine($"✓ INCR dotnet:counter = {count}");

    // EXISTS / TTL
    var exists = await keys.ExistsAsync("dotnet:hello");
    var ttl = await keys.TTLAsync("dotnet:hello");
    Console.WriteLine($"✓ EXISTS = {exists}, TTL = {ttl}");

    // CLEANUP
    await keys.DelAsync("dotnet:hello", "dotnet:counter");

    Console.WriteLine("\n=== 全部完成 ===");
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ 错误: {ex.Message}");
    Console.WriteLine($"  可能原因: Redis 服务器未启动或连接配置不正确");
}
finally
{
    client.Dispose();
}
