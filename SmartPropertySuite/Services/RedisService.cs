using SmartPropertySuite.IServices;
using SmartPropertySuite.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace SmartPropertySuite.Services
{
    public class RedisService : IRedisService
    {
        private readonly IDatabase _db;

        public RedisService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task<ChatState> GetStateAsync(string userId)
        {
            var value = await _db.StringGetAsync(userId);
            return value.IsNullOrEmpty ? new ChatState() : JsonSerializer.Deserialize<ChatState>(value);
        }

        public async Task SaveStateAsync(string userId, ChatState state)
        {
            var json = JsonSerializer.Serialize(state);
            await _db.StringSetAsync(userId, json, TimeSpan.FromHours(1));
        }

        public async Task RemoveStateAsync(string userId)
        {
            await _db.KeyDeleteAsync(userId);
        }
    }
}
