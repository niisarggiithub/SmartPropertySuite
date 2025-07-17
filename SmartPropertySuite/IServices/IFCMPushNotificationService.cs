using SmartPropertySuite.Models;
using SmartPropertySuite.Models.DatabaseModels;

namespace SmartPropertySuite.IServices
{
    public interface IFCMPushNotificationService
    {
        Task<string> SendPushNotification(FCMPushNotification notification);
        Task AddOrUpdateFCMPushNotificationPayload(CRMPropertyMobileUserDeviceInfo notification);
        CRMPropertyMobileUserDeviceInfo GetFCMMobileDeviceInfoByEmail(string email);
    }
}
