using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Oauth2.v2;
using SmartPropertySuite.IServices;
using SmartPropertySuite.Models.DatabaseModels;
using Microsoft.EntityFrameworkCore;

namespace SmartPropertySuite.Services
{
    public class TokenService : ITokenService
    {
        private readonly ApplicationDbContext.ApplicationDbContext _dbContext;
        private const string ClientSecretFile = "/app/google-calendar-ouath-creds.json";

        public TokenService(ApplicationDbContext.ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task RefreshAccessTokenAsync(CRMPropertySuiteUserInfo userInfo)
        {
            try
            {
                var secrets = GoogleClientSecrets.FromFile(ClientSecretFile).Secrets;

                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = secrets,
                    Scopes = new[] { CalendarService.Scope.Calendar, Oauth2Service.Scope.UserinfoEmail }
                });

                var token = new TokenResponse { RefreshToken = userInfo.RefreshToken };

                var credential = new UserCredential(flow, "user", token);

                bool success = await credential.RefreshTokenAsync(CancellationToken.None);

                if (!success)
                {
                    throw new InvalidOperationException("Failed to refresh the access token.");
                }

                var newInfo = new CRMPropertySuiteUserInfo
                {
                    Email = userInfo.Email,
                    AccessToken = credential.Token.AccessToken,
                    RefreshToken = credential.Token.RefreshToken,
                    ExpiryTime = DateTime.Now.AddSeconds(credential.Token.ExpiresInSeconds ?? 0)
                };

                await SaveTokensToDatabase(newInfo);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public async Task SaveTokensToDatabase(CRMPropertySuiteUserInfo userInfo)
        {
            try
            {
                var entity = await _dbContext.CRMPropertySuiteUserInfo.FirstOrDefaultAsync(u => u.Email == userInfo.Email);

                if (entity == null)
                {
                    await _dbContext.CRMPropertySuiteUserInfo.AddAsync(userInfo);
                }
                else
                {
                    entity.AccessToken = userInfo.AccessToken;
                    entity.RefreshToken = userInfo.RefreshToken;
                    entity.ExpiryTime = userInfo.ExpiryTime;
                }

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }
    }
}
