using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using SmartPropertySuite.IServices;
using SmartPropertySuite.Models;
using SmartPropertySuite.Models.DatabaseModels;

namespace SmartPropertySuite.Services
{
    public class FCMPushNotificationService : IFCMPushNotificationService
    {
        private readonly ApplicationDbContext.ApplicationDbContext _dbContext;
        private readonly string filepath = Path.Combine(AppContext.BaseDirectory, "pushservice.json");

        public FCMPushNotificationService(ApplicationDbContext.ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile(filepath),
                });
            }
        }

        public Task<string> SendPushNotification(FCMPushNotification notification)
        {
            if (notification.IsAndroidDevice == true)
            {
                return FirebaseMessaging.DefaultInstance.SendAsync(
                    GetAndroidPayload(notification));
            }
            return FirebaseMessaging.DefaultInstance.SendAsync(
                   GetIOSPayload(notification));
        }

        public async Task AddOrUpdateFCMPushNotificationPayload(CRMPropertyMobileUserDeviceInfo notification)
        {
            var entity = _dbContext.CRMPropertyMobileUserDeviceInfo.Where(x => x.Email == notification.Email).FirstOrDefault();

            if (entity == null)
            {
                await _dbContext.CRMPropertyMobileUserDeviceInfo.AddAsync(notification);
            }
            else
            {
                entity.IsAndroidDevice = notification.IsAndroidDevice;
                entity.DeviceId = notification.DeviceId;
            }

            _dbContext.SaveChanges();
        }

        public CRMPropertyMobileUserDeviceInfo GetFCMMobileDeviceInfoByEmail(string email)
        {
            return _dbContext.CRMPropertyMobileUserDeviceInfo.Where(x => x.Email == email).FirstOrDefault()!;
        }

        private Message GetAndroidPayload(FCMPushNotification notification)
        {
            return new Message()
            {
                Token = notification.DeviceId,
                Notification = new Notification()
                {
                    Title = notification.Title,
                    Body = notification.Body,
                },
                Data = new Dictionary<string, string>
                {
                    { "chatid", notification.ChatId.ToString() }
                }
            };
        }

        private Message GetIOSPayload(FCMPushNotification notification)
        {

            return new Message()
            {
                Token = notification.DeviceId,
                Apns = GetApnsConfig(notification),
            };
        }

        private ApnsConfig GetApnsConfig(FCMPushNotification notification)
        {
            var customData = new Dictionary<string, object>
            {
                { "chatid", notification.ChatId.ToString() }
            };

            return new ApnsConfig()
            {
                Aps = new Aps()
                {
                    MutableContent = true,
                    Sound = "default",
                    ContentAvailable = true,
                    CustomData = customData,
                    Alert = new ApsAlert()
                    {
                        Title = notification.Title,
                        Body = notification.Body,
                    },
                },
            };
        }
    }
}
