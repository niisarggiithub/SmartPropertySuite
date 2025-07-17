using Microsoft.AspNetCore.Mvc;
using SmartPropertySuite.IServices;
using SmartPropertySuite.Models;
using SmartPropertySuite.Models.DatabaseModels;

namespace SmartPropertySuite.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FCMPushnotificationController : Controller
    {
        private readonly ApplicationDbContext.ApplicationDbContext _dbContext;
        private readonly IFCMPushNotificationService _pushNotificationService;

        public FCMPushnotificationController(ApplicationDbContext.ApplicationDbContext dbContext, IFCMPushNotificationService pushNotificationService)
        {
            _dbContext = dbContext;
            _pushNotificationService = pushNotificationService;
        }

        [HttpPost("FCMPushNotification")]
        public async Task<IActionResult> AddFCMNotificationDetails([FromBody] FCMPushNotification pushNotification)
        {
            try
            {
                var notification = new CRMPropertyMobileUserDeviceInfo
                {
                    DeviceId = pushNotification.DeviceId,
                    IsAndroidDevice = pushNotification.IsAndroidDevice,
                    Email = pushNotification.Email,
                };

                await _pushNotificationService.AddOrUpdateFCMPushNotificationPayload(notification);

                return Ok();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }
    }
}
