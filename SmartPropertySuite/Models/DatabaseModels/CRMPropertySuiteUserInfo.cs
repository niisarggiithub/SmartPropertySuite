using System.ComponentModel.DataAnnotations.Schema;

namespace SmartPropertySuite.Models.DatabaseModels
{
    [Table("CRMPropertySuiteUserInfo")]
    public class CRMPropertySuiteUserInfo
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiryTime { get; set; }
    }
}
