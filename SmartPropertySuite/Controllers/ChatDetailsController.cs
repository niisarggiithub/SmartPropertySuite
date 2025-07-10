using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPropertySuite.Models;
using SmartPropertySuite.Models.DatabaseModels;
using System.Text.Json;

namespace SmartPropertySuite.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatDetailsController : Controller
    {
        private readonly ApplicationDbContext.ApplicationDbContext _dbContext;

        public ChatDetailsController(ApplicationDbContext.ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("userChat")]
        public async Task<ActionResult<CRMPropertySuiteUserChats>> GetChatsForUser([FromQuery] string email)
        {
            try
            {
                var chats = await _dbContext.CRMPropertySuiteUserChats
                .Where(x => x.Email == email)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

                return Ok(chats);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        [HttpGet("chatId")]
        public async Task<ActionResult<CRMPropertySuiteUserChats>> GetChatByChatId([FromQuery] Guid chatId)
        {
            try
            {
                var chat = await _dbContext.CRMPropertySuiteUserChats
                .Include(c => c.Conversations)
                    .ThenInclude(conv => conv.Messages)
                .FirstOrDefaultAsync(x => x.ChatId == chatId);

                return Ok(chat);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        [HttpGet("conversations")]
        public async Task<ActionResult<List<CRMPropertySuiteUserConversations>>> GetConversationsById([FromQuery] Guid chatId)
        {
            try
            {
                var conversations = await _dbContext.CRMPropertySuiteUserConversations
                .Where(c => c.ChatId == chatId && c.Status == 1)
                .Include(c => c.Messages)
                .OrderByDescending(c => c.StartedAt)
                .ToListAsync();

                var result = conversations.Select(c => new
                {
                    c.Id,
                    c.ConversationId,
                    c.ChatId,
                    c.StartedAt,
                    c.EndedAt,
                    c.Status,
                    ExtractionResult = string.IsNullOrWhiteSpace(c.ExtractionResult)
                        ? null
                        : JsonSerializer.Deserialize<ExtractionResult>(c.ExtractionResult),
                    Messages = c.Messages
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        [HttpGet("messages")]
        public async Task<ActionResult<List<CRMPropertySuiteUserChatMessages>>> GetMessagesById([FromQuery] Guid conversationId)
        {
            try
            {
                var messages = await _dbContext.CRMPropertySuiteUserChatMessages
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.SentAt)
                .GroupBy(m => m.ConversationId)
                .ToListAsync();

                return Ok(messages);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }
    }
}
