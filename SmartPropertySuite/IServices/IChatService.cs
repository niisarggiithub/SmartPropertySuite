using SmartPropertySuite.Models;

namespace SmartPropertySuite.IServices
{
    public interface IChatService
    {
        Task<string> GetBotReplyAsync(string input, string systemPrompt);
        Task<ExtractionResult> ExtractUserDetailsAsync(string input);
        Task<string> ExtractIntentAsync(string input);
    }
}
