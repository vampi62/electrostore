using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Services.SessionService;

public interface ISessionService
{
    public string GetClientIp();

    public UserRole GetClientRole();

    public int GetClientId();

    public string GetTokenId();

    public string GetTokenAuthMethod();
}