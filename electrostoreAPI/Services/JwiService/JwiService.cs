using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.SessionService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ElectrostoreAPI.Services.JwiService;

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

    public async Task SaveToken(Jwt token, int userId, string reason = "user_password", Guid? sessionId = null, int? externalLoginId = null)
    {
        var clientIp = _sessionService.GetClientIp();
        if (sessionId is null)
        {
            var session = new UsersSessions
            {
                id_user_session = Guid.NewGuid(),
                id_user = userId,
                auth_method = reason,
                id_user_external_login = externalLoginId,
                created_by_ip = clientIp
            };
            await _context.UsersSessions.AddAsync(session);
            sessionId = session.id_user_session;
        }
        else if (!await _context.UsersSessions.AnyAsync(s => s.id_user_session == sessionId && s.id_user == userId))
        {
            throw new KeyNotFoundException($"Session with id '{sessionId}' not found for user '{userId}'");
        }
        await _context.JwtTokens.AddRangeAsync(
            new JwtTokens
            {
                id_jwi = token.token_id,
                id_user_session = sessionId.Value,
                token_type = TokenType.Access,
                expires_at = token.expire_date_token,
                auth_method = reason,
                created_by_ip = clientIp
            },
            new JwtTokens
            {
                id_jwi = token.refresh_token_id,
                id_user_session = sessionId.Value,
                token_type = TokenType.Refresh,
                expires_at = token.expire_date_refresh_token,
                auth_method = reason,
                created_by_ip = clientIp
            });
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
        var tokenType = role switch
        {
            "access" => TokenType.Access,
            "refresh" => TokenType.Refresh,
            _ => (TokenType?)null
        };
        if (tokenType is null) return true;
        var jwt = _context.JwtTokens
            .Where(t => t.id_jwi == Guid.Parse(tokenId) && t.token_type == tokenType)
            .Select(t => new { t.revoked_at, t.expires_at, session_revoked_at = t.Session!.revoked_at })
            .FirstOrDefault();
        if (jwt is null) return true;
        return jwt.revoked_at is not null || jwt.session_revoked_at is not null || jwt.expires_at <= DateTime.UtcNow;
    }

    public async Task RevokeAllAccessTokenByUser(int userId, string reason)
    {
        var clientIp = _sessionService.GetClientIp();
        var tokens = await _context.JwtTokens
            .Where(t => t.Session!.id_user == userId && t.token_type == TokenType.Access && t.revoked_at == null && t.expires_at > DateTime.UtcNow)
            .ToListAsync();
        RevokeTokens(tokens, reason, clientIp);
        await _context.SaveChangesAsync();
    }

    public async Task RevokeAllRefreshTokenByUser(int userId, string reason)
    {
        var clientIp = _sessionService.GetClientIp();
        var tokens = await _context.JwtTokens
            .Where(t => t.Session!.id_user == userId && t.token_type == TokenType.Refresh && t.revoked_at == null && t.expires_at > DateTime.UtcNow)
            .ToListAsync();
        RevokeTokens(tokens, reason, clientIp);
        // without any valid refresh token a session can't be renewed anymore
        var sessions = await _context.UsersSessions
            .Where(s => s.id_user == userId && s.revoked_at == null)
            .ToListAsync();
        foreach (var session in sessions)
        {
            session.revoked_at = DateTime.UtcNow;
            session.revoked_by_ip = clientIp;
        }
        await _context.SaveChangesAsync();
    }

    public async Task<PaginatedResponseDto<SessionDto>> GetTokenSessionsByUserId(int userId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null)
    {
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != userId && clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to view this session.");
        }
        rsql ??= [];
        rsql.Add(new FilterDto { field = "id_user", search_type = "eq", value = userId.ToString() });
        var page = await _context.UsersSessions
            .ToPagedQuery(limit, offset, rsql, sort, new SorterDto { field = "created_at", order = "desc" })
            .ToResponseAsync(sessions => sessions.ToList());
        var sessions = page.data.ToList();
        var lastRefreshTokens = await GetLastRefreshTokens(sessions.Select(s => s.id_user_session).ToList());
        return new PaginatedResponseDto<SessionDto>
        {
            data = sessions.Select(s => ToSessionDto(s, lastRefreshTokens.GetValueOrDefault(s.id_user_session))).ToList(),
            pagination = page.pagination,
            filters = page.filters,
            sort = page.sort
        };
    }

    public async Task<SessionDto> GetTokenSessionById(string id, int userId)
    {
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != userId && clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to view this session.");
        }
        return await FindSession(Guid.Parse(id), userId) ?? throw new KeyNotFoundException($"Session with id '{id}' not found for user '{userId}'");
    }

    public async Task<Guid> GetSessionIdByTokenId(string id, int userId)
    {
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != userId && clientRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to view this session.");
        }
        var token = await _context.JwtTokens
            .Where(t => t.id_jwi == Guid.Parse(id) && t.token_type == TokenType.Refresh && t.Session!.id_user == userId)
            .FirstOrDefaultAsync();
        return token?.id_user_session ?? Guid.Empty;
    }

    public async Task<SessionDto> RevokeSessionById(string id, string reason, int userId)
    {
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != userId && clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to revoke this session.");
        }
        var sessionId = Guid.Parse(id);
        var clientIp = _sessionService.GetClientIp();
        var session = await _context.UsersSessions
            .Where(s => s.id_user == userId && s.id_user_session == sessionId)
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Session with id '{id}' not found for user '{userId}'");
        var tokens = await _context.JwtTokens
            .Where(t => t.id_user_session == sessionId && t.revoked_at == null && t.expires_at > DateTime.UtcNow)
            .ToListAsync();
        RevokeTokens(tokens, reason, clientIp);
        if (session.revoked_at is null)
        {
            session.revoked_at = DateTime.UtcNow;
            session.revoked_by_ip = clientIp;
        }
        await _context.SaveChangesAsync();
        return await FindSession(sessionId, userId) ?? throw new KeyNotFoundException($"Session with id '{id}' not found for user '{userId}'");
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
        var jwt_refresh = await _context.JwtTokens
            .Include(t => t.Session)
            .FirstOrDefaultAsync(t => t.id_jwi == Guid.Parse(refreshToken) && t.token_type == TokenType.Refresh);
        if ((jwt_refresh?.Session is null) || (userId is not null && jwt_refresh.Session.id_user != userId))
        {
            throw new KeyNotFoundException($"RefreshToken with id '{refreshToken}' not found");
        }
        // a session only holds one live access/refresh pair at a time, so the pair of the refresh token is every live token of its session
        var tokens = await _context.JwtTokens
            .Where(t => t.id_user_session == jwt_refresh.id_user_session && t.revoked_at == null)
            .ToListAsync();
        RevokeTokens(tokens, reason, clientIp);
        await _context.SaveChangesAsync();
    }

    private static void RevokeTokens(IEnumerable<JwtTokens> tokens, string reason, string clientIp)
    {
        foreach (var token in tokens)
        {
            token.revoked_at = DateTime.UtcNow;
            token.revoked_by_ip = clientIp;
            token.revoked_reason = reason;
        }
    }

    // the session lifetime is the one of its latest refresh token
    private async Task<Dictionary<Guid, RefreshTokenRow>> GetLastRefreshTokens(List<Guid> sessionIds)
    {
        var refreshTokens = await _context.JwtTokens
            .Where(t => sessionIds.Contains(t.id_user_session) && t.token_type == TokenType.Refresh)
            .Select(t => new RefreshTokenRow { id_user_session = t.id_user_session, expires_at = t.expires_at, created_at = t.created_at, revoked_reason = t.revoked_reason })
            .ToListAsync();
        return refreshTokens
            .GroupBy(t => t.id_user_session)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.expires_at).First());
    }

    private static SessionDto ToSessionDto(UsersSessions session, RefreshTokenRow? lastRefreshToken)
    {
        return new SessionDto
        {
            session_id = session.id_user_session,
            expires_at = lastRefreshToken?.expires_at ?? session.created_at,
            is_revoked = session.revoked_at is not null,
            auth_method = session.auth_method,
            created_at = lastRefreshToken?.created_at ?? session.created_at,
            created_by_ip = session.created_by_ip,
            revoked_at = session.revoked_at,
            revoked_by_ip = session.revoked_by_ip,
            revoked_reason = lastRefreshToken?.revoked_reason,
            id_user = session.id_user,
            first_created_at = session.created_at
        };
    }

    private async Task<SessionDto?> FindSession(Guid sessionId, int userId)
    {
        var session = await _context.UsersSessions
            .FirstOrDefaultAsync(s => s.id_user == userId && s.id_user_session == sessionId);
        if (session is null)
        {
            return null;
        }
        var lastRefreshToken = (await GetLastRefreshTokens([sessionId])).GetValueOrDefault(sessionId);
        return ToSessionDto(session, lastRefreshToken);
    }

    private sealed class RefreshTokenRow
    {
        public Guid id_user_session { get; init; }
        public DateTime expires_at { get; init; }
        public DateTime created_at { get; init; }
        public string? revoked_reason { get; init; }
    }
}
