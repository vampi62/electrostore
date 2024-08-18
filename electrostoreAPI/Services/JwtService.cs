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

    public string GenerateToken(ReadUserDto user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.Key);
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.id_user.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.id_user.ToString()),
            new Claim(ClaimTypes.Name, user.email_user),
        };
        var roles = user.role_user.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}