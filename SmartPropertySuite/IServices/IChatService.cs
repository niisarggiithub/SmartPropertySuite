using SmartPropertySuite.Models;
using System.Threading.Tasks;

namespace SmartPropertySuite.IServices
{
    public interface IChatService
    {
        Task<string> GetBotReplyAsync(string input, string systemPrompt, string isExtraction = null);
        Task<ExtractionResult> ExtractUserDetailsAsync(string input);
        Task<string> GenerateChatTitleAsync(List<string> messages);
        Task<string> ExtractIntentAsync(string input);
    }
}
