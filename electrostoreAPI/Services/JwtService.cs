using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using electrostore.Dto;
using electrostore.Enums;

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
        if (user is null) throw new ArgumentNullException(nameof(user));

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.Key);

        // Access token
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.id_user.ToString()),
            new(ClaimTypes.NameIdentifier, user.id_user.ToString()),
            new(ClaimTypes.Name, user.email_user),
            new(ClaimTypes.Role, "access"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        switch (user.role_user)
        {
            case UserRole.Admin:
                claims.Add(new Claim(ClaimTypes.Role, "admin"));
                break;
            case UserRole.User:
                claims.Add(new Claim(ClaimTypes.Role, "user"));
                break;
            case UserRole.Moderator:
                claims.Add(new Claim(ClaimTypes.Role, "moderator"));
                break;
            default:
                claims.Add(new Claim(ClaimTypes.Role, "guest"));
                break;
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
            new(JwtRegisteredClaimNames.Sub, user.id_user.ToString()),
            new(ClaimTypes.NameIdentifier, user.id_user.ToString()),
            new(ClaimTypes.Name, user.email_user),
            new(ClaimTypes.Role, "refresh"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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

        // return token
        return new JWT
        {
            token = tokenString,
            expire_date_token = tokenDescriptor.Expires ?? DateTime.UtcNow.AddDays(_jwtSettings.ExpireDays),
            refresh_token = refreshTokenString,
            expire_date_refresh_token = refreshTokenDescriptor.Expires ?? DateTime.UtcNow.AddDays(_jwtSettings.ExpireDays + 7),
            token_id = Guid.Parse(token.Id),
            refresh_token_id = Guid.Parse(refreshToken.Id),
            created_at = DateTime.UtcNow
        };
    }
}