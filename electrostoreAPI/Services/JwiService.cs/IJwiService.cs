using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.JwiService;

public interface IJwiService
{
    public Task SaveToken(JWT token, int userId, string clientIp);
    public bool IsRevoked(string tokenId, string type);
    public bool ValidateToken(string token, string role);
    public Task InvalidateAllAccessTokenByUser(int userId);
    public Task InvalidateAllRefreshTokenByUser(int userId);
    public Task InvalidateRefreshTokenById(string tokenId);
    public Task InvalidateAccessTokenById(string tokenId);

}