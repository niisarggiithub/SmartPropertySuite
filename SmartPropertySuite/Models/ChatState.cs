using System.Text.Json.Serialization;

namespace SmartPropertySuite.Models
{
#nullable disable
    public class ChatState
    {
        public string IssueType { get; set; }
        public string Priority { get; set; }
        public string ContactEmail { get; set; }

        [JsonIgnore]
        public List<TimeSlot> AvailableSlots { get; set; } = new();
        public int? PreferredSlotIndex { get; internal set; }
        public bool IsComplete() =>
                !string.IsNullOrEmpty(IssueType) &&
                !string.IsNullOrEmpty(Priority) &&
                !string.IsNullOrEmpty(ContactEmail) &&
                PreferredSlotIndex.HasValue;
    }
#nullable restore
}
