using AuthMetodology.Infrastructure.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace AuthMetodology.Infrastructure.Services
{
    public class RedisService : IRedisService
    {
        private readonly IDatabase cache;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

        public RedisService(ConnectionMultiplexer connection)
        {
            cache = connection.GetDatabase();
        }
        public async Task<T> GetStringFromCacheAsync<T>(string key)
        {
            var result = await cache.StringGetAsync(key);
            if (result.IsNullOrEmpty)
                return default;

            return JsonSerializer.Deserialize<T>(result);
        }

        public async Task RemoveStringFromCacheAsync(string key)
        {
            await cache.KeyDeleteAsync(key);
        }

        public async Task SetStringToCacheAsync<T>(string key, T value)
        {
            var jsonData = JsonSerializer.Serialize(value);
            await cache.StringSetAsync(key, jsonData, CacheExpiration);
        }
    }
}
