using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using electrostore.Dto;

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

        // return token
        return new JWT
        {
            token = tokenString,
            expire_date_token = tokenDescriptor.Expires ?? DateTime.UtcNow.AddDays(_jwtSettings.ExpireDays),
            refesh_token = refreshTokenString,
            expire_date_refresh_token = refreshTokenDescriptor.Expires ?? DateTime.UtcNow.AddDays(_jwtSettings.ExpireDays + 7),
            token_id = Guid.Parse(token.Id),
            refresh_token_id = Guid.Parse(refreshToken.Id),
            created_at = DateTime.UtcNow
        };
    }
}