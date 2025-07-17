using System.ComponentModel.DataAnnotations.Schema;

namespace SmartPropertySuite.Models.DatabaseModels
{
    [Table("CRMPropertyMobileUserDeviceInfo")]
    public class CRMPropertyMobileUserDeviceInfo
    {
        public int Id { get; set; }

        public string DeviceId { get; set; }

        public bool IsAndroidDevice { get; set; }

        public string Email { get; set; }
    }
}
