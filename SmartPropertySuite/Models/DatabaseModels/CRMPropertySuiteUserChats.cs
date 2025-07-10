using System.ComponentModel.DataAnnotations.Schema;

namespace SmartPropertySuite.Models.DatabaseModels
{
    [Table("CRMPropertySuiteUserChats")]
    public class CRMPropertySuiteUserChats
    {
        public int Id { get; set; }
        public Guid ChatId { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<CRMPropertySuiteUserConversations> Conversations { get; set; }
    }
}
