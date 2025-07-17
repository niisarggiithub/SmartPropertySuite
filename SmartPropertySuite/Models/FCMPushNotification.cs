namespace SmartPropertySuite.Models
{
#nullable disable
    public class FCMPushNotification
    {
        public string DeviceId { get; set; }

        public bool IsAndroidDevice { get; set; }

        public string Title { get; set; }

        public string Body { get; set; }

        public string Email { get; set; }

        public Guid ChatId { get; set; }
    }
#nullable restore
}
