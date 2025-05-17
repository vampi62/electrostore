using electrostore.Dto;

namespace electrostore.Services.JwiService;

public interface IJwiService
{
    public Task SaveToken(JWT token, int userId, Guid? sessionId = null);

    public bool IsRevoked(string tokenId, string role);

    public bool ValidateToken(string token, string role);

    public Task RevokeAllAccessTokenByUser(int userId, string reason);

    public Task RevokeAllRefreshTokenByUser(int userId, string reason);

    public Task<IEnumerable<ReadRefreshTokenDto>> GetTokenSessionsByUserId(int userId, int limit, int offset, bool showRevoked = false, bool showExpired = false);

    public Task<int> GetTokenSessionsCountByUserId(int userId);

    public Task<ReadRefreshTokenDto> GetTokenSessionById(string id, int userId, bool showRevoked = false, bool showExpired = false);

    public Task<Guid> GetSessionIdByTokenId(string id, int userId);

    public Task RevokeSessionById(string id, string reason, int userId);

    public Task RevokePairTokenByRefreshToken(string refreshToken, string reason, int? userId = null);
}