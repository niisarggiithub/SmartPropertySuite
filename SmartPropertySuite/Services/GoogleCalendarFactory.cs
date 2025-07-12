using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using SmartPropertySuite.IServices;
using Google.Apis.Oauth2.v2;

namespace SmartPropertySuite.Services
{
    public class GoogleCalendarFactory : IGoogleCalendarFactory
    {
        private string _clientId;
        private string _clientSecret;

        public GoogleCalendarFactory(IConfiguration configuration)
        {
            _clientId = configuration["GoogleOAuthCredential:ClientId"];
            _clientSecret = configuration["GoogleOAuthCredential:ClientSecret"];
        }

        public GoogleAuthorizationCodeFlow CreateFlow()
        {
            try
            {
                return new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = _clientId,
                        ClientSecret = _clientSecret
                    },
                    Scopes = new[] { CalendarService.Scope.Calendar, Oauth2Service.Scope.UserinfoEmail }
                });
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public UserCredential CreateCredential(string email, string accessToken, string refreshToken, DateTime expiryTime)
        {
            try
            {
                var tokenResponse = new TokenResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresInSeconds = (long)(expiryTime - DateTime.Now).TotalSeconds
                };

                var flow = CreateFlow();
                return new UserCredential(flow, email, tokenResponse);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public CalendarService CreateCalendarService(UserCredential credential)
        {
            try
            {
                return new CalendarService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "SmartPropertySuite"
                });
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }
    }
}
