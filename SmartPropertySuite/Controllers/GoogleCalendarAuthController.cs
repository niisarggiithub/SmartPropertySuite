using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net.Http.Headers;
using Google.Apis.Auth.OAuth2.Responses;
using SmartPropertySuite.Models;
using SmartPropertySuite.IServices;
using SmartPropertySuite.Models.DatabaseModels;

namespace SmartPropertySuite.Controllers
{
    [ApiController]
    [Route("auth")]
    public class GoogleCalendarAuthController : Controller
    {
        private string ClientSecretFile = Path.Combine(AppContext.BaseDirectory, "google-calendar-ouath-creds.json");
        private const string RedirectUri = "http://localhost:54321/auth/callback";
        private readonly ITokenService _tokenService;
        private readonly IGoogleCalendarFactory _googleCalendar;

        public GoogleCalendarAuthController(ITokenService tokenService, IGoogleCalendarFactory googleCalendar)
        {
            _tokenService = tokenService;
            _googleCalendar = googleCalendar;
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            var secrets = GoogleClientSecrets.FromFile(ClientSecretFile).Secrets;
            var scopes = Uri.EscapeDataString("openid https://www.googleapis.com/auth/calendar email");
            var redirect = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                           $"client_id={secrets.ClientId}&" +
                           $"redirect_uri={RedirectUri}&" +
                           $"response_type=code&" +
                           $"scope={scopes}&" +
                           $"access_type=offline&" +
                           $"prompt=consent";

            return Redirect(redirect);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> AuthCallback([FromQuery] string code)
        {
            try
            {
                var flow = _googleCalendar.CreateFlow();

                TokenResponse tokenResponse = await flow.ExchangeCodeForTokenAsync(
                    userId: "user",
                    code: code,
                    redirectUri: RedirectUri,
                    CancellationToken.None
                );

                using var http = new HttpClient();
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);

                var userInfoResponse = await http.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");
                var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();

                var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(userInfoJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var email = userInfo.Email;

                var expiresIn = tokenResponse.ExpiresInSeconds;
                var expiryTime = DateTime.Now.AddSeconds(expiresIn ?? 3600);

                var info = new CRMPropertySuiteUserInfo
                {
                    Email = email,
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    ExpiryTime = expiryTime
                };

                await _tokenService.SaveTokensToDatabase(info);

                return Ok("Authenticated with Google. Tokens saved.");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }
    }
}
