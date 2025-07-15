
namespace SmartPropertySuite.Models
{
#nullable disable
    public class ChatRequest
    {
        public string UserEmail { get; set; } // The email of the user sending the message
        public string Message { get; set; } // The message content sent by the user
        public bool IsNewConversation { get; set; } // true if this is the first message in a new conversation, false if it's a follow-up message
        public Guid ChatId { get; set; } // unique identifier for the chat session, created by the mobile team
        public Guid ConversationId { get; set; } // unique identifier for the conversation, created by the mobile team
        public string Sender { get; set; } // e.g., "user" or "assistant"
    }
#nullable restore
}
