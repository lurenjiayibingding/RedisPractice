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

        [TestMethod]
        public async Task TestSetAsync()
        {
            RedisClient redisClient = new RedisClient("127.0.0.1", 6379, "");
            await redisClient.ConnectAsync();
            var result = await redisClient.SetAsync("name", "Tom");
            Assert.IsTrue(string.Equals(result, "ok", StringComparison.InvariantCultureIgnoreCase));
        }

        [TestMethod]
        public async Task TestGetAsync()
        {
            RedisClient redisClient = new RedisClient("127.0.0.1", 6379, "");
            await redisClient.ConnectAsync();
            await redisClient.SetAsync("name", "Tom");
            var result = await redisClient.GetAsync<string>("name");
            Assert.IsTrue(string.Equals(result, "Tom", StringComparison.InvariantCulture));
        }
    }
}
