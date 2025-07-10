using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Calendar.v3;

namespace SmartPropertySuite.IServices
{
    public interface IGoogleCalendarFactory
    {
        GoogleAuthorizationCodeFlow CreateFlow();
        UserCredential CreateCredential(string email, string accessToken, string refreshToken, DateTime expiryTime);
        CalendarService CreateCalendarService(UserCredential credential);
    }
}
