using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SmartPropertySuite.Models.DatabaseModels
{
    [Table("CRMPropertySuiteUserChatMessages")]
    public class CRMPropertySuiteUserChatMessages
    {
        public int Id { get; set; }
        public Guid MessageId { get; set; }
        public Guid ConversationId { get; set; }
        public string Sender { get; set; } // "user" or "assistant"
        public string MessageText { get; set; }
        public DateTime? SentAt { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(ConversationId))]
        public CRMPropertySuiteUserConversations Conversation { get; set; }
    }
}
