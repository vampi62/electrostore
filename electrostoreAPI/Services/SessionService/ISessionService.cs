using electrostore.Enums;

namespace electrostore.Services.SessionService;

public interface ISessionService
{
    public string GetClientIp();

    public UserRole GetClientRole();

    public int GetClientId();
}