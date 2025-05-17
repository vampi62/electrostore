using AutoMapper;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using electrostore.Dto;
using Microsoft.EntityFrameworkCore;
using electrostore.Models;
using electrostore.Enums;
using electrostore.Services.SessionService;

namespace electrostore.Services.JwiService;

public class JwiService : IJwiService
{
    private readonly IMapper _mapper;
    private readonly JwtSettings _jwtSettings;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;

    public JwiService(IMapper mapper, ApplicationDbContext context, IOptions<JwtSettings> jwtSettings, ISessionService sessionService)
    {
        _mapper = mapper;
        _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
        _context = context;
        _sessionService = sessionService;
    }

    public async Task SaveToken(JWT token, int userId, Guid? sessionId = null)
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
            created_by_ip = clientIp,
            id_user = userId,
            session_id = (Guid)sessionId,
        };
        var jwi_refresh = new JwiRefreshTokens
        {
            id_jwi_refresh = token.refresh_token_id,
            expires_at = token.expire_date_refresh_token,
            is_revoked = false,
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
    private JwtSecurityToken readToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
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
        SecurityToken securityToken;
        tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
        return (JwtSecurityToken)securityToken;
    }

    public bool ValidateToken(string token, string role)
    {
        if (string.IsNullOrEmpty(token)) return false;
        try
        {
            var tokenOBJ = readToken(token);
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
            var jwi_access = _context.JwiAccessTokens.FirstOrDefault(x => x.id_jwi_access == Guid.Parse(tokenId));
            if (jwi_access is null) return true;
            return jwi_access.is_revoked;
        }
        else if (role == "refresh")
        {
            var jwi_refresh = _context.JwiRefreshTokens.FirstOrDefault(x => x.id_jwi_refresh == Guid.Parse(tokenId));
            if (jwi_refresh is null) return true;
            return jwi_refresh.is_revoked;
        }
        return true;
    }

    public async Task RevokeAllAccessTokenByUser(int userId, string reason)
    {
        var clientIp = _sessionService.GetClientIp();
        var jwi_access = await _context.JwiAccessTokens
            .Where(x => x.id_user == userId && !x.is_revoked && x.expires_at > DateTime.UtcNow)
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
            .Where(x => x.id_user == userId && !x.is_revoked && x.expires_at > DateTime.UtcNow)
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

    public async Task<IEnumerable<ReadRefreshTokenDto>> GetTokenSessionsByUserId(int userId, int limit, int offset, bool showRevoked = false, bool showExpired = false)
    {
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != userId && clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to view this session.");
        }
        var query = _context.JwiRefreshTokens
            .Where(x => x.id_user == userId);
        if (!showRevoked)
        {
            query = query.Where(x => !x.is_revoked);
        }
        if (!showExpired)
        {
            query = query.Where(x => x.expires_at > DateTime.UtcNow);
        }
        query = query.OrderByDescending(x => x.created_at);
        query = query.GroupBy(x => x.session_id).Select(group => group.First());
        query = query.Skip(offset).Take(limit);
        var sessions = await query.ToListAsync();
        return _mapper.Map<IEnumerable<ReadRefreshTokenDto>>(sessions);
    }

    public async Task<int> GetTokenSessionsCountByUserId(int userId)
    {
        var count = await _context.JwiRefreshTokens
            .Where(x => x.id_user == userId && !x.is_revoked && x.expires_at > DateTime.UtcNow)
            .GroupBy(x => x.session_id)
            .CountAsync();
        return count;
    }

    public async Task<ReadRefreshTokenDto> GetTokenSessionById(string id, int userId, bool showRevoked = false, bool showExpired = false)
    {
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != userId && clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to view this session.");
        }
        var query = _context.JwiRefreshTokens
            .Where(x => x.id_user == userId && x.id_jwi_refresh == Guid.Parse(id));
        if (!showRevoked)
        {
            query = query.Where(x => !x.is_revoked);
        }
        if (!showExpired)
        {
            query = query.Where(x => x.expires_at > DateTime.UtcNow);
        }
        query = query.OrderByDescending(x => x.created_at);
        query = query.GroupBy(x => x.session_id).Select(group => group.First());
        var session = await query.FirstOrDefaultAsync();
        return _mapper.Map<ReadRefreshTokenDto>(session);
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
            .Where(x => x.id_user == userId && x.id_jwi_refresh == Guid.Parse(id))
            .FirstOrDefaultAsync();
        return session?.session_id ?? Guid.Empty;
    }

    public async Task RevokeSessionById(string id, string reason, int userId)
    {
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != userId && clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to revoke this session.");
        }
        var clientIp = _sessionService.GetClientIp();
        var jwi_refresh = await _context.JwiRefreshTokens
            .Where(x => x.id_user == userId && x.session_id == Guid.Parse(id) && !x.is_revoked && x.expires_at > DateTime.UtcNow)
            .FirstOrDefaultAsync();
        if (jwi_refresh is not null)
        {
            jwi_refresh.is_revoked = true;
            jwi_refresh.revoked_at = DateTime.UtcNow;
            jwi_refresh.revoked_by_ip = clientIp;
            jwi_refresh.revoked_reason = reason;
        }
        var jwi_access = await _context.JwiAccessTokens
            .Where(x => x.id_user == userId && x.session_id == Guid.Parse(id) && !x.is_revoked && x.expires_at > DateTime.UtcNow)
            .FirstOrDefaultAsync();
        if (jwi_access is not null)
        {
            jwi_access.is_revoked = true;
            jwi_access.revoked_at = DateTime.UtcNow;
            jwi_access.revoked_by_ip = clientIp;
            jwi_access.revoked_reason = reason;
        }
        await _context.SaveChangesAsync();
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
        if (!await _context.Users.AnyAsync(x => x.id_user == userId))
        {
            throw new KeyNotFoundException($"User with id {userId} not found");
        }
        var jwi_refresh = await _context.JwiRefreshTokens.FindAsync(Guid.Parse(refreshToken));
        if ((jwi_refresh is null) || (userId is not null && jwi_refresh.id_user != userId))
        {
            throw new KeyNotFoundException($"RefreshToken with id {refreshToken} not found");
        }
        var jwi_access = await _context.JwiAccessTokens.FindAsync(jwi_refresh.id_jwi_access) ?? throw new KeyNotFoundException($"AccessToken with id {jwi_refresh.id_jwi_access} not found");
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