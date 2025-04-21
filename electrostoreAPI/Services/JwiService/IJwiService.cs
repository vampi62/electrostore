using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.JwiService;

public interface IJwiService
{
    public Task SaveToken(JWT token, int userId, string clientIp);

    public bool IsRevoked(string tokenId, string type);

    public bool ValidateToken(string token, string role);

    public Task RevokeAllAccessTokenByUser(int userId, string clientIp, string reason);

    public Task RevokeAllRefreshTokenByUser(int userId, string clientIp, string reason);

    public Task RevokeRefreshTokenById(string tokenId, string clientIp, string reason, int? userId = null);

    public Task RevokeAccessTokenById(string tokenId, string clientIp, string reason, int? userId = null);

    public Task RevokePairTokenByRefreshToken(string refreshToken, string clientIp, string reason, int? userId = null);

    public Task<IEnumerable<ReadAccessTokenDto>> GetAccessTokensByUserId(int userId, int limit = 100, int offset = 0);

    public Task<ReadAccessTokenDto> GetAccessTokenByToken(int userId, string token);

    public Task<int> GetAccessTokensCountByUserId(int userId);

    public Task<IEnumerable<ReadRefreshTokenDto>> GetRefreshTokensByUserId(int userId, int limit = 100, int offset = 0);

    public Task<ReadRefreshTokenDto> GetRefreshTokenByToken(int userId, string token);

    public Task<int> GetRefreshTokensCountByUserId(int userId);
}