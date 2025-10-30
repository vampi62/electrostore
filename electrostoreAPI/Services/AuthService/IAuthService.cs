using electrostore.Dto;

namespace electrostore.Services.AuthService;

public interface IAuthService
{
    public Task<SsoUrlResponse> GetSSOAuthUrl(string sso_method);
    public Task<LoginResponse> LoginWithSSO(string sso_method, SsoLoginRequest request);

    public Task<bool> CheckUserPasswordByEmail(string email, string password);

    public Task<bool> CheckUserPasswordById(int id, string password);

    public Task ForgotPassword(ForgotPasswordRequest request);

    public Task ResetPassword(ResetPasswordRequest request);

    public Task<LoginResponse> LoginWithPassword(LoginRequest request);

    public Task<LoginResponse> RefreshJwt();
}