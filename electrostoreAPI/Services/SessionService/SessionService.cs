using electrostore.Enums;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace electrostore.Services.SessionService;

public class SessionService : ISessionService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    public string GetClientIp()
    {
        var httpContext = _httpContextAccessor.HttpContext ?? throw new HttpRequestException("HttpContext is null");
        var clientIp = "";
        if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out Microsoft.Extensions.Primitives.StringValues value))
        {
            var forwardedIps = value.ToString();
            var ip = forwardedIps.Split(',').FirstOrDefault();
            clientIp = ip?.Trim();
        }
        if (string.IsNullOrWhiteSpace(clientIp) && httpContext.Request.Headers.TryGetValue("X-Real-IP", out value))
        {
            clientIp = value.ToString();
        }
        if (string.IsNullOrWhiteSpace(clientIp))
        {
            clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        }
        return clientIp;
    }

    public UserRole GetClientRole()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userRole = UserRole.User;
        if (httpContext == null || httpContext.User == null)
        {
            return userRole;
        }
        if (httpContext.User.IsInRole("admin"))
        {
            userRole = UserRole.Admin;
        }
        else if (httpContext.User.IsInRole("moderator"))
        {
            userRole = UserRole.Moderator;
        }
        return userRole;
    }

    public int GetClientId()
    {
        var httpContext = _httpContextAccessor.HttpContext ?? throw new HttpRequestException("HttpContext is null");
        var userId = 0;
        if (httpContext.User == null)
        {
            return userId;
        }
        var claim = httpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out int id))
        {
            userId = id;
        }
        return userId;
    }

    public string GetTokenId()
    {
        var httpContext = _httpContextAccessor.HttpContext ?? throw new HttpRequestException("HttpContext is null");
        var tokenId = string.Empty;
        if (httpContext.User == null)
        {
            return tokenId;
        }
        var claim = httpContext.User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
        if (claim != null && Guid.TryParse(claim.Value, out Guid id))
        {
            tokenId = id.ToString();
        }
        return tokenId;
    }
}