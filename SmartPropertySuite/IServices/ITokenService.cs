using SmartPropertySuite.Models.DatabaseModels;

namespace SmartPropertySuite.IServices
{
    public interface ITokenService
    {
        Task RefreshAccessTokenAsync(CRMPropertySuiteUserInfo userInfo);
        Task SaveTokensToDatabase(CRMPropertySuiteUserInfo userInfo);
    }
}
