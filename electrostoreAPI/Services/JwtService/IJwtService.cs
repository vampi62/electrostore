using electrostore.Dto;

namespace electrostore.Services.JwtService;

public interface IJwtService
{
    public Task<Jwt> GenerateToken(ReadUserDto user, string reason);
}