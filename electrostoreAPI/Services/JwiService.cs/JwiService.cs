using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using electrostore.Dto;
using Microsoft.EntityFrameworkCore;
using electrostore.Models;

namespace electrostore.Services.JwiService;

public class JwiService : IJwiService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ApplicationDbContext _context;

    public JwiService(ApplicationDbContext context, IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
        _context = context;
    }

    public async Task SaveToken(JWT token, int userId, string clientIp)
    {
        var jwi_access = new JWIAccessToken
        {
            id_jwi_access = token.token_id,
            expires_at = token.expire_date_token,
            is_revoked = false,
            created_at = DateTime.UtcNow,
            created_by_ip = clientIp,
            id_user = userId
        };
        var jwi_refresh = new JWIRefreshToken
        {
            id_jwi_refresh = token.refresh_token_id,
            expires_at = token.expire_date_refresh_token,
            is_revoked = false,
            created_at = DateTime.UtcNow,
            created_by_ip = clientIp,
            id_user = userId,
            id_jwi_access = jwi_access.id_jwi_access
        };
        await _context.JWIRefreshToken.AddAsync(jwi_refresh);
        await _context.JWIAccessToken.AddAsync(jwi_access);
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
            if (tokenOBJ == null) return false;
            var claims = tokenOBJ.Claims.ToList();
            if (claims.FirstOrDefault(x => x.Type == ClaimTypes.Role && x.Value == role) == null) return false;
            if (IsRevoked(tokenOBJ.Id,role)) return false;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsRevoked(string id, string role)
    {
        if (string.IsNullOrEmpty(id)) return true;
        if (role == "access")
        {
            var jwi_access = _context.JWIAccessToken.FirstOrDefault(x => x.id_jwi_access == Guid.Parse(id));
            if (jwi_access == null) return true;
            return jwi_access.is_revoked;
        }
        else if (role == "refresh")
        {
            var jwi_refresh = _context.JWIRefreshToken.FirstOrDefault(x => x.id_jwi_refresh == Guid.Parse(id));
            if (jwi_refresh == null) return true;
            return jwi_refresh.is_revoked;
        }
        return true;
    }

    public async Task InvalidateAllAccessTokenByUser(int id_user)
    {
        var jwi_access = await _context.JWIAccessToken
            .Where(x => x.id_user == id_user && x.is_revoked == false && x.expires_at > DateTime.UtcNow)
            .ToListAsync();
        foreach (var jwi in jwi_access)
        {
            jwi.is_revoked = true;
        }
        await _context.SaveChangesAsync();
    }

    public async Task InvalidateAllRefreshTokenByUser(int id_user)
    {
        var jwi_refresh = await _context.JWIRefreshToken
            .Where(x => x.id_user == id_user && x.is_revoked == false && x.expires_at > DateTime.UtcNow)
            .ToListAsync();
        foreach (var jwi in jwi_refresh)
        {
            jwi.is_revoked = true;
        }
        await _context.SaveChangesAsync();
    }

    public async Task InvalidateRefreshTokenById(string token)
    {
        var jwi_refresh = await _context.JWIRefreshToken
            .FirstOrDefaultAsync(x => x.id_jwi_refresh == Guid.Parse(token));
        if (jwi_refresh != null)
        {
            jwi_refresh.is_revoked = true;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task InvalidateAccessTokenById(string token)
    {
        var jwi_access = await _context.JWIAccessToken
            .FirstOrDefaultAsync(x => x.id_jwi_access == Guid.Parse(token));
        if (jwi_access != null)
        {
            jwi_access.is_revoked = true;
            await _context.SaveChangesAsync();
        }
    }
}