using SmartPropertySuite.Models.DatabaseModels;

namespace SmartPropertySuite.IServices
{
    public interface IChatDetails
    {
        Task AddChat(CRMPropertySuiteUserChats chat);
        Task<CRMPropertySuiteUserChats> GetChatByChatId(Guid chatId);
        Task AddOrUpdateCoversations(List<CRMPropertySuiteUserConversations> conversations);
        Task AddMessages(List<CRMPropertySuiteUserChatMessages> messages);
        Task<List<CRMPropertySuiteUserChatMessages>> GetMessagesById(Guid conversationId);
    }
}
