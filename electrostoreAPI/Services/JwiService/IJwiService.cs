using electrostore.Dto;

namespace electrostore.Services.JwiService;

public interface IJwiService
{
    public Task SaveToken(JWT token, int userId);

    public bool IsRevoked(string tokenId, string role);

    public bool ValidateToken(string token, string role);

    public Task RevokeAllAccessTokenByUser(int userId, string reason);

    public Task RevokeAllRefreshTokenByUser(int userId, string reason);

    public Task RevokeRefreshTokenById(string token, string reason, int? userId = null);

    public Task RevokeAccessTokenById(string token, string reason, int? userId = null);

    public Task RevokePairTokenByRefreshToken(string refreshToken, string reason, int? userId = null);

    public Task<IEnumerable<ReadAccessTokenDto>> GetAccessTokensByUserId(int userId, int limit = 100, int offset = 0);

    public Task<ReadAccessTokenDto> GetAccessTokenByToken(int userId, string token);

    public Task<int> GetAccessTokensCountByUserId(int userId);

    public Task<IEnumerable<ReadRefreshTokenDto>> GetRefreshTokensByUserId(int userId, int limit = 100, int offset = 0);

    public Task<ReadRefreshTokenDto> GetRefreshTokenByToken(int userId, string token);

    public Task<int> GetRefreshTokensCountByUserId(int userId);
}