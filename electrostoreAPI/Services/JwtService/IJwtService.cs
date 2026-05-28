using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.JwtService;

public interface IJwtService
{
    public Task<Jwt> GenerateToken(ReadUserDto user, string reason);
}