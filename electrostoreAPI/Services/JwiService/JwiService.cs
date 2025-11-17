using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using electrostore.Dto;
using Microsoft.EntityFrameworkCore;
using electrostore.Models;
using electrostore.Enums;
using electrostore.Services.SessionService;
using Microsoft.IdentityModel.JsonWebTokens;

namespace electrostore.Services.JwiService;

public class JwiService : IJwiService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;

    public JwiService(ApplicationDbContext context, IOptions<JwtSettings> jwtSettings, ISessionService sessionService)
    {
        _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
        _context = context;
        _sessionService = sessionService;
    }

    public async Task SaveToken(Jwt token, int userId, string reason = "user_password", Guid? sessionId = null)
    {
        // check if sessionId is null
        if (sessionId is null)
        {
            sessionId = Guid.NewGuid();
        }
        var clientIp = _sessionService.GetClientIp();
        var jwi_access = new JwiAccessTokens
        {
            id_jwi_access = token.token_id,
            expires_at = token.expire_date_token,
            is_revoked = false,
            auth_method = reason,
            created_by_ip = clientIp,
            id_user = userId,
            session_id = (Guid)sessionId,
        };
        var jwi_refresh = new JwiRefreshTokens
        {
            id_jwi_refresh = token.refresh_token_id,
            expires_at = token.expire_date_refresh_token,
            is_revoked = false,
            auth_method = reason,
            created_by_ip = clientIp,
            id_user = userId,
            session_id = (Guid)sessionId,
            id_jwi_access = jwi_access.id_jwi_access
        };
        await _context.JwiRefreshTokens.AddAsync(jwi_refresh);
        await _context.JwiAccessTokens.AddAsync(jwi_access);
        await _context.SaveChangesAsync();
    }

    // build token
    private JsonWebToken ReadToken(string token)
    {
        var tokenHandler = new JsonWebTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.Key);
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            ValidateLifetime = true
        };
        var result = tokenHandler.ValidateTokenAsync(token, tokenValidationParameters).Result;
        return (JsonWebToken)result.SecurityToken;
    }

    public bool ValidateToken(string token, string role)
    {
        if (string.IsNullOrEmpty(token)) return false;
        try
        {
            var tokenOBJ = ReadToken(token);
            if (tokenOBJ is null) return false;
            var claims = tokenOBJ.Claims.ToList();
            if (!claims.Any(x => x.Type == "role" && x.Value == role)) return false;
            if (IsRevoked(tokenOBJ.Id, role)) return false;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsRevoked(string tokenId, string role)
    {
        if (string.IsNullOrEmpty(tokenId)) return true;
        if (role == "access")
        {
            var jwi_access = _context.JwiAccessTokens.FirstOrDefault(jwi => jwi.id_jwi_access == Guid.Parse(tokenId));
            if (jwi_access is null) return true;
            return jwi_access.is_revoked;
        }
        else if (role == "refresh")
        {
            var jwi_refresh = _context.JwiRefreshTokens.FirstOrDefault(jwi => jwi.id_jwi_refresh == Guid.Parse(tokenId));
            if (jwi_refresh is null) return true;
            return jwi_refresh.is_revoked;
        }
        return true;
    }

    public async Task RevokeAllAccessTokenByUser(int userId, string reason)
    {
        var clientIp = _sessionService.GetClientIp();
        var jwi_access = await _context.JwiAccessTokens
            .Where(jwi => jwi.id_user == userId && !jwi.is_revoked && jwi.expires_at > DateTime.UtcNow)
            .ToListAsync();
        foreach (var jwi in jwi_access)
        {
            jwi.is_revoked = true;
            jwi.revoked_at = DateTime.UtcNow;
            jwi.revoked_by_ip = clientIp;
            jwi.revoked_reason = reason;
        }
        await _context.SaveChangesAsync();
    }

    public async Task RevokeAllRefreshTokenByUser(int userId, string reason)
    {
        var clientIp = _sessionService.GetClientIp();
        var jwi_refresh = await _context.JwiRefreshTokens
            .Where(jwi => jwi.id_user == userId && !jwi.is_revoked && jwi.expires_at > DateTime.UtcNow)
            .ToListAsync();
        foreach (var jwi in jwi_refresh)
        {
            jwi.is_revoked = true;
            jwi.revoked_at = DateTime.UtcNow;
            jwi.revoked_by_ip = clientIp;
            jwi.revoked_reason = reason;
        }
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<SessionDto>> GetTokenSessionsByUserId(int userId, int limit, int offset, bool showRevoked = false, bool showExpired = false)
    {
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != userId && clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to view this session.");
        }
        var query = _context.JwiRefreshTokens
            .Where(jwi => jwi.id_user == userId);
        var groupedQuery = query
            .GroupBy(jwi => jwi.session_id)
            .Select(group => new SessionDto
            {
                session_id = group.Key,
                expires_at = group.OrderByDescending(jwi => jwi.expires_at).First().expires_at,
                is_revoked = group.OrderByDescending(jwi => jwi.expires_at).First().is_revoked,
                auth_method = group.OrderByDescending(jwi => jwi.expires_at).First().auth_method,
                created_at = group.OrderByDescending(jwi => jwi.expires_at).First().created_at,
                created_by_ip = group.OrderByDescending(jwi => jwi.expires_at).First().created_by_ip,
                revoked_at = group.OrderByDescending(jwi => jwi.expires_at).First().revoked_at,
                revoked_by_ip = group.OrderByDescending(jwi => jwi.expires_at).First().revoked_by_ip,
                revoked_reason = group.OrderByDescending(jwi => jwi.expires_at).First().revoked_reason,
                id_user = group.OrderByDescending(jwi => jwi.expires_at).First().id_user,
                first_created_at = group.OrderBy(jwi => jwi.expires_at).First().created_at
            })
            .OrderByDescending(jwi => jwi.created_at)
            .Skip(offset)
            .Take(limit);
        var sessions = await groupedQuery.ToListAsync();
        if (!showRevoked)
        {
            sessions = sessions.Where(s => !s.is_revoked).ToList();
        }
        if (!showExpired)
        {
            sessions = sessions.Where(s => s.expires_at > DateTime.UtcNow).ToList();
        }
        return sessions;
    }

    public async Task<int> GetTokenSessionsCountByUserId(int userId, bool showRevoked = false, bool showExpired = false)
    {
        var count = await _context.JwiRefreshTokens
            .Where(jwi => jwi.id_user == userId &&
                (showRevoked || !jwi.is_revoked) &&
                (showExpired || jwi.expires_at > DateTime.UtcNow))
            .GroupBy(jwi => jwi.session_id)
            .CountAsync();
        return count;
    }

    public async Task<SessionDto> GetTokenSessionById(string id, int userId, bool showRevoked = false, bool showExpired = false)
    {
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != userId && clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to view this session.");
        }
        var query = _context.JwiRefreshTokens
            .Where(jwi => jwi.id_user == userId && jwi.session_id == Guid.Parse(id));
        var groupedQuery = query
            .GroupBy(jwi => jwi.session_id)
            .Select(group => new SessionDto
            {
                session_id = group.Key,
                expires_at = group.OrderByDescending(jwi => jwi.expires_at).First().expires_at,
                is_revoked = group.OrderByDescending(jwi => jwi.expires_at).First().is_revoked,
                auth_method = group.OrderByDescending(jwi => jwi.expires_at).First().auth_method,
                created_at = group.OrderByDescending(jwi => jwi.expires_at).First().created_at,
                created_by_ip = group.OrderByDescending(jwi => jwi.expires_at).First().created_by_ip,
                revoked_at = group.OrderByDescending(jwi => jwi.expires_at).First().revoked_at,
                revoked_by_ip = group.OrderByDescending(jwi => jwi.expires_at).First().revoked_by_ip,
                revoked_reason = group.OrderByDescending(jwi => jwi.expires_at).First().revoked_reason,
                id_user = group.OrderByDescending(jwi => jwi.expires_at).First().id_user,
                first_created_at = group.OrderBy(jwi => jwi.expires_at).First().created_at
            });
        return await groupedQuery.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Session with id '{id}' not found for user '{userId}'");
    }

    public async Task<Guid> GetSessionIdByTokenId(string id, int userId)
    {
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != userId && clientRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to view this session.");
        }
        var session = await _context.JwiRefreshTokens
            .Where(jwi => jwi.id_user == userId && jwi.id_jwi_refresh == Guid.Parse(id))
            .FirstOrDefaultAsync();
        return session?.session_id ?? Guid.Empty;
    }

    public async Task<SessionDto> RevokeSessionById(string id, string reason, int userId)
    {
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != userId && clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to revoke this session.");
        }
        var clientIp = _sessionService.GetClientIp();
        var jwi_refresh = await _context.JwiRefreshTokens
            .Where(jwi => jwi.id_user == userId && jwi.session_id == Guid.Parse(id) && !jwi.is_revoked && jwi.expires_at > DateTime.UtcNow)
            .FirstOrDefaultAsync();
        if (jwi_refresh is not null)
        {
            jwi_refresh.is_revoked = true;
            jwi_refresh.revoked_at = DateTime.UtcNow;
            jwi_refresh.revoked_by_ip = clientIp;
            jwi_refresh.revoked_reason = reason;
        }
        var jwi_access = await _context.JwiAccessTokens
            .Where(jwi => jwi.id_user == userId && jwi.session_id == Guid.Parse(id) && !jwi.is_revoked && jwi.expires_at > DateTime.UtcNow)
            .FirstOrDefaultAsync();
        if (jwi_access is not null)
        {
            jwi_access.is_revoked = true;
            jwi_access.revoked_at = DateTime.UtcNow;
            jwi_access.revoked_by_ip = clientIp;
            jwi_access.revoked_reason = reason;
        }
        await _context.SaveChangesAsync();
        return await _context.JwiRefreshTokens
            .Where(jwi => jwi.id_user == userId && jwi.session_id == Guid.Parse(id))
            .GroupBy(jwi => jwi.session_id)
            .Select(group => new SessionDto
            {
                session_id = group.Key,
                expires_at = group.OrderByDescending(jwi => jwi.expires_at).First().expires_at,
                is_revoked = group.OrderByDescending(jwi => jwi.expires_at).First().is_revoked,
                auth_method = group.OrderByDescending(jwi => jwi.expires_at).First().auth_method,
                created_at = group.OrderByDescending(jwi => jwi.expires_at).First().created_at,
                created_by_ip = group.OrderByDescending(jwi => jwi.expires_at).First().created_by_ip,
                revoked_at = group.OrderByDescending(jwi => jwi.expires_at).First().revoked_at,
                revoked_by_ip = group.OrderByDescending(jwi => jwi.expires_at).First().revoked_by_ip,
                revoked_reason = group.OrderByDescending(jwi => jwi.expires_at).First().revoked_reason,
                id_user = group.OrderByDescending(jwi => jwi.expires_at).First().id_user,
                first_created_at = group.OrderBy(jwi => jwi.expires_at).First().created_at
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Session with id '{id}' not found for user '{userId}'");     
    }

    public async Task RevokePairTokenByRefreshToken(string refreshToken, string reason, int? userId = null)
    {
        var clientIp = _sessionService.GetClientIp();
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != userId && clientRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException($"You are not authorized to access this resource");
        }
        if (!await _context.Users.AnyAsync(u => u.id_user == userId))
        {
            throw new KeyNotFoundException($"User with id '{userId}' not found");
        }
        var jwi_refresh = await _context.JwiRefreshTokens.FindAsync(Guid.Parse(refreshToken));
        if ((jwi_refresh is null) || (userId is not null && jwi_refresh.id_user != userId))
        {
            throw new KeyNotFoundException($"RefreshToken with id '{refreshToken}' not found");
        }
        var jwi_access = await _context.JwiAccessTokens.FindAsync(jwi_refresh.id_jwi_access) ?? throw new KeyNotFoundException($"AccessToken with id '{jwi_refresh.id_jwi_access}' not found");
        jwi_refresh.is_revoked = true;
        jwi_refresh.revoked_at = DateTime.UtcNow;
        jwi_refresh.revoked_by_ip = clientIp;
        jwi_refresh.revoked_reason = reason;
        jwi_access.is_revoked = true;
        jwi_access.revoked_at = DateTime.UtcNow;
        jwi_access.revoked_by_ip = clientIp;
        jwi_access.revoked_reason = reason;
        await _context.SaveChangesAsync();
    }
}