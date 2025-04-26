using AutoMapper;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using electrostore.Dto;
using Microsoft.EntityFrameworkCore;
using electrostore.Models;

namespace electrostore.Services.JwiService;

public class JwiService : IJwiService
{
    private readonly IMapper _mapper;
    private readonly JwtSettings _jwtSettings;
    private readonly ApplicationDbContext _context;

    public JwiService(IMapper mapper, ApplicationDbContext context, IOptions<JwtSettings> jwtSettings)
    {
        _mapper = mapper;
        _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
        _context = context;
    }

    public async Task SaveToken(JWT token, int userId, string clientIp)
    {
        var jwi_access = new JwiAccessTokens
        {
            id_jwi_access = token.token_id,
            expires_at = token.expire_date_token,
            is_revoked = false,
            created_by_ip = clientIp,
            id_user = userId
        };
        var jwi_refresh = new JwiRefreshTokens
        {
            id_jwi_refresh = token.refresh_token_id,
            expires_at = token.expire_date_refresh_token,
            is_revoked = false,
            created_by_ip = clientIp,
            id_user = userId,
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
            if (IsRevoked(tokenOBJ.Id,role)) return false;
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

    public async Task RevokeAllAccessTokenByUser(int userId, string clientIp, string reason)
    {
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

    public async Task RevokeAllRefreshTokenByUser(int userId, string clientIp, string reason)
    {
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

    public async Task RevokeRefreshTokenById(string token, string clientIp, string reason, int? userId = null)
    {
        var jwi_refresh = await _context.JwiRefreshTokens.FindAsync(Guid.Parse(token));
        if ((jwi_refresh is null) || (userId is not null && jwi_refresh.id_user != userId))
        {
            throw new KeyNotFoundException($"RefreshToken with id {token} not found");
        }
        jwi_refresh.is_revoked = true;
        jwi_refresh.revoked_at = DateTime.UtcNow;
        jwi_refresh.revoked_by_ip = clientIp;
        jwi_refresh.revoked_reason = reason;
        await _context.SaveChangesAsync();
    }
    
    public async Task RevokeAccessTokenById(string token, string clientIp, string reason, int? userId = null)
    {
        var jwi_access = await _context.JwiAccessTokens.FindAsync(Guid.Parse(token));
        if ((jwi_access is null) || (userId is not null && jwi_access.id_user != userId))
        {
            throw new KeyNotFoundException($"AccessToken with id {token} not found");
        }
        jwi_access.is_revoked = true;
        jwi_access.revoked_at = DateTime.UtcNow;
        jwi_access.revoked_by_ip = clientIp;
        jwi_access.revoked_reason = reason;
        await _context.SaveChangesAsync();
    }
    
    public async Task RevokePairTokenByRefreshToken(string refreshToken, string clientIp, string reason, int? userId = null)
    {
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

    public async Task<IEnumerable<ReadAccessTokenDto>> GetAccessTokensByUserId(int userId, int limit = 100, int offset = 0)
    {
        if (!await _context.Users.AnyAsync(x => x.id_user == userId))
        {
            throw new KeyNotFoundException($"User with id {userId} not found");
        }
        var query = _context.JwiAccessTokens.AsQueryable();
        query = query.Where(x => x.id_user == userId);
        query = query.OrderByDescending(x => x.created_at);
        query = query.Skip(offset).Take(limit);
        var JWIAccess = await query.ToListAsync();
        return _mapper.Map<List<ReadAccessTokenDto>>(JWIAccess);
    }

    public async Task<ReadAccessTokenDto> GetAccessTokenByToken(int userId, string token)
    {
        var jwi_access = await _context.JwiAccessTokens.FindAsync(Guid.Parse(token));
        if (jwi_access is null || jwi_access.id_user != userId)
        {
            throw new KeyNotFoundException($"AccessToken with id {token} not found");
        }
        return _mapper.Map<ReadAccessTokenDto>(jwi_access);
    }

    public async Task<int> GetAccessTokensCountByUserId(int userId)
    {
        if (!await _context.Users.AnyAsync(x => x.id_user == userId))
        {
            throw new KeyNotFoundException($"User with id {userId} not found");
        }
        return await _context.JwiAccessTokens
            .Where(x => x.id_user == userId)
            .CountAsync();
    }

    public async Task<IEnumerable<ReadRefreshTokenDto>> GetRefreshTokensByUserId(int userId, int limit = 100, int offset = 0)
    {
        if (!await _context.Users.AnyAsync(x => x.id_user == userId))
        {
            throw new KeyNotFoundException($"User with id {userId} not found");
        }
        var query = _context.JwiRefreshTokens.AsQueryable();
        query = query.Where(x => x.id_user == userId);
        query = query.OrderByDescending(x => x.created_at);
        query = query.Skip(offset).Take(limit);
        var JWIRefresh = await query.ToListAsync();
        return _mapper.Map<List<ReadRefreshTokenDto>>(JWIRefresh);
    }

    public async Task<ReadRefreshTokenDto> GetRefreshTokenByToken(int userId, string token)
    {
        var jwi_refresh = await _context.JwiRefreshTokens.FindAsync(Guid.Parse(token));
        if (jwi_refresh is null || jwi_refresh.id_user != userId)
        {
            throw new KeyNotFoundException($"RefreshToken with id {token} not found");
        }
        return _mapper.Map<ReadRefreshTokenDto>(jwi_refresh);
    }

    public async Task<int> GetRefreshTokensCountByUserId(int userId)
    {
        if (!await _context.Users.AnyAsync(x => x.id_user == userId))
        {
            throw new KeyNotFoundException($"User with id {userId} not found");
        }
        return await _context.JwiRefreshTokens
            .Where(x => x.id_user == userId)
            .CountAsync();
    }
}