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
            var redisClient = await RedisClient.CreateClientAsync("127.0.0.1", 6380, "sa", "123qwe");
            await redisClient.ConnectAsync();
            var result = await redisClient.PingAsync();
            Assert.IsNotNull(result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestConnection()
        {
            var redisClient = await RedisClient.CreateClientAsync("127.0.0.1", 6380, "", "");
            await redisClient.ConnectAsync();
            var result = await redisClient.PingAsync();
            Assert.IsTrue(string.Equals(result, "OK", StringComparison.InvariantCultureIgnoreCase));
        }

        [TestMethod]
        public async Task TestSetAsync()
        {
            var redisClient = await RedisClient.CreateClientAsync("127.0.0.1", 6379, "", "");
            await redisClient.ConnectAsync();
            var result = await redisClient.SetAsync("name", "Tom");
            Assert.IsTrue(string.Equals(result, "ok", StringComparison.InvariantCultureIgnoreCase));
        }

        [TestMethod]
        public async Task TestGetAsync()
        {
            var redisClient = await RedisClient.CreateClientAsync("127.0.0.1", 6379, "", "");
            await redisClient.ConnectAsync();
            await redisClient.SetAsync("name", "Tom");
            var result = await redisClient.GetAsync("name");
            Assert.IsTrue(string.Equals(result, "Tom", StringComparison.InvariantCulture));
        }

        [TestMethod]
        public async Task TestGetAsync2()
        {
            var redisClient = await RedisClient.CreateClientAsync("127.0.0.1", 6379, "", "");
            await redisClient.ConnectAsync();
            await redisClient.SetAsync("count", "100");
            var result = await redisClient.IncrAsync("count");
            Assert.AreEqual(102, result);
        }
    }
}
