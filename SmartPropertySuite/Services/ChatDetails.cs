using Microsoft.EntityFrameworkCore;
using SmartPropertySuite.IServices;
using SmartPropertySuite.Models.DatabaseModels;

namespace SmartPropertySuite.Services
{
    public class ChatDetails : IChatDetails
    {
        private readonly ApplicationDbContext.ApplicationDbContext _dbContext;

        public ChatDetails(ApplicationDbContext.ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddChat(CRMPropertySuiteUserChats chat)
        {
            var entity = await _dbContext.CRMPropertySuiteUserChats.FirstOrDefaultAsync(u => u.ChatId == chat.ChatId);

            if (entity == null)
            {
                await _dbContext.CRMPropertySuiteUserChats.AddAsync(chat);
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task<CRMPropertySuiteUserChats> GetChatByChatId(Guid chatId)
        {
            return await _dbContext.CRMPropertySuiteUserChats.FirstOrDefaultAsync(u => u.ChatId == chatId);
        }

        public async Task AddOrUpdateCoversations(List<CRMPropertySuiteUserConversations> conversations)
        {
            foreach (var conversation in conversations)
            {
                var entity = await _dbContext.CRMPropertySuiteUserConversations.FirstOrDefaultAsync(u => u.ConversationId == conversation.ConversationId);

                if (entity == null)
                {
                    await _dbContext.CRMPropertySuiteUserConversations.AddAsync(conversation);
                }
                else
                {
                    entity.Status = conversation.Status;
                    entity.EndedAt = conversation.EndedAt;
                    entity.ExtractionResult = conversation.ExtractionResult;
                }

                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task AddMessages(List<CRMPropertySuiteUserChatMessages> messages)
        {
            foreach (var message in messages)
            {
                var entity = await _dbContext.CRMPropertySuiteUserChatMessages.FirstOrDefaultAsync(u => u.MessageId == message.MessageId);

                if (entity == null)
                {
                    await _dbContext.CRMPropertySuiteUserChatMessages.AddAsync(message);
                }

                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<List<CRMPropertySuiteUserChatMessages>> GetMessagesById(Guid conversationId)
        {
            return await _dbContext.CRMPropertySuiteUserChatMessages
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();
        }
    }
}
