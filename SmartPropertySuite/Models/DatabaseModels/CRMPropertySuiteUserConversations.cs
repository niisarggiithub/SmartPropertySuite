using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SmartPropertySuite.Models.DatabaseModels
{
    [Table("CRMPropertySuiteUserConversations")]
    public class CRMPropertySuiteUserConversations
    {
        public int Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid ChatId { get; set; }  // Foreign key to CRMPropertySuiteUserChats
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int Status { get; set; } // e.g., 0 = Closed, 1 = Open, etc.
        public string ExtractionResult { get; set; } // NEW: JSON blob of ExtractionResult

        [JsonIgnore]
        [ForeignKey(nameof(ChatId))]
        public CRMPropertySuiteUserChats Chat { get; set; }
        public List<CRMPropertySuiteUserChatMessages> Messages { get; set; }
    }
}
