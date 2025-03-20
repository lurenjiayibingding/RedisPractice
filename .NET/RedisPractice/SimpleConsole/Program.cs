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
                RedisClient redisClient = new RedisClient("127.0.0.1", 6379, "");
                await redisClient.ConnectAsync();
                var result = await redisClient.SetAsync("name", "Tom");
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
