using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.JwtService;

public class JwtService
{
    private readonly JwtSettings _jwtSettings;

    public JwtService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
    }

    public JWT GenerateToken(ReadUserDto user)
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
            new Claim(ClaimTypes.Role, "access")
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

        // Refresh token
        var refreshClaims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.id_user.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.id_user.ToString()),
            new Claim(ClaimTypes.Name, user.email_user),
            new Claim(ClaimTypes.Role, "refresh")
        };
        var refreshTokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(refreshClaims),
            Expires = DateTime.UtcNow.AddDays(_jwtSettings.ExpireDays + 7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
        };

        return new JWT
        {
            token = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor)),
            expire_date_token = tokenDescriptor.Expires?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty,
            refesh_token = tokenHandler.WriteToken(tokenHandler.CreateToken(refreshTokenDescriptor)),
            expire_date_refresh_token = refreshTokenDescriptor.Expires?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty
        };
    }

    public bool ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return false;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.Key);
        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            }, out SecurityToken validatedToken);
        }
        catch
        {
            return false;
        }

        return true;
    }


    public bool ValidateRole(string token, string role)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(role)) return false;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.Key);
        try
        {
            // check if the token contains the role
            var tokenS = tokenHandler.ReadToken(token) as JwtSecurityToken;
            if (tokenS == null) return false;
            var roleClaim = tokenS.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role && x.Value == role);
            return roleClaim != null;
        }
        catch
        {
            return false;
        }
    }
}