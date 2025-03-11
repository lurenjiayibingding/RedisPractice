using SimpleRedis;
using System.Threading.Tasks;

namespace Test
{
    [TestClass]
    public sealed class RedisClientTest
    {
        [TestMethod]
        public async Task TestPingAsync()
        {
            RedisClient redisClient = new RedisClient("127.0.0.1", 6379, "");
            await redisClient.ConnectAsync();
            var result = await redisClient.PingAsync();
            Assert.IsNotNull(result);
        }
    }
}
