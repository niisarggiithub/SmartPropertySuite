using SmartPropertySuite.Models;

namespace SmartPropertySuite.IServices
{
    public interface IRedisService
    {
        Task<ChatState> GetStateAsync(string userId);
        Task SaveStateAsync(string userId, ChatState state);
        Task RemoveStateAsync(string userId);
    }
}
