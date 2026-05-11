using SimpleRedis;
using System.Formats.Tar;

namespace SimpleConsole
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                RedisClient redisClient = await RedisClient.GetClientAndConnectAsync("127.0.0.1", 6379, "", "");

                var redisComment = new RedisCommand(redisClient);
                var result = await redisComment.SetAsync("name", "Tom");
                Console.WriteLine(result);
                Console.WriteLine("Hello, World!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetType().FullName);
            }
        }
    }
}
