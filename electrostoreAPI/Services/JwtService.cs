using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using electrostore.Dto;
using Microsoft.EntityFrameworkCore;
using electrostore.Models;
using System.Runtime.CompilerServices;

namespace electrostore.Services.JwtService;

public class JwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ApplicationDbContext _context;

    public JwtService(ApplicationDbContext context, IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
        _context = context;
    }

    public async Task<JWT> GenerateToken(ReadUserDto user, string IPUser)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.Key);

        // Access token
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.id_user.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.id_user.ToString()),
            new Claim(ClaimTypes.Name, user.email_user),
            new Claim(ClaimTypes.Role, "access"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var roles = user.role_user.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(_jwtSettings.ExpireDays),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        // Refresh token
        var refreshClaims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.id_user.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.id_user.ToString()),
            new Claim(ClaimTypes.Name, user.email_user),
            new Claim(ClaimTypes.Role, "refresh"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var refreshTokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(refreshClaims),
            Expires = DateTime.UtcNow.AddDays(_jwtSettings.ExpireDays + 7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
        };
        var refreshToken = tokenHandler.CreateToken(refreshTokenDescriptor);
        var refreshTokenString = tokenHandler.WriteToken(refreshToken);

        // save to database
        var jwi_access = new JWIAccessToken
        {
            id_jwi_access = Guid.Parse(token.Id),
            expires_at = tokenDescriptor.Expires ?? DateTime.UtcNow.AddDays(_jwtSettings.ExpireDays),
            is_revoked = false,
            created_at = DateTime.UtcNow,
            created_by_ip = IPUser,
            id_user = user.id_user
        };
        var jwi_refresh = new JWIRefreshToken
        {
            id_jwi_refresh = Guid.Parse(refreshToken.Id),
            expires_at = refreshTokenDescriptor.Expires ?? DateTime.UtcNow.AddDays(_jwtSettings.ExpireDays + 7),
            is_revoked = false,
            created_at = DateTime.UtcNow,
            created_by_ip = IPUser,
            id_user = user.id_user
        };
        await _context.JWIRefreshToken.AddAsync(jwi_refresh);
        await _context.JWIAccessToken.AddAsync(jwi_access);
        await _context.SaveChangesAsync();

        // return token
        return new JWT
        {
            token = tokenString,
            expire_date_token = tokenDescriptor.Expires?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty,
            refesh_token = refreshTokenString,
            expire_date_refresh_token = refreshTokenDescriptor.Expires?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty
        };
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

    public async Task InvalidateAllAccessTokenForUser(int id_user)
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

    public async Task InvalidateAllRefreshTokenForUser(int id_user)
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
}