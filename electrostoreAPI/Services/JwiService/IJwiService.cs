using electrostore.Dto;

namespace electrostore.Services.JwiService;

public interface IJwiService
{
    public Task SaveToken(Jwt token, int userId, string reason = "user_password", Guid? sessionId = null);

    public bool IsRevoked(string tokenId, string role);

    public bool ValidateToken(string token, string role);

    public Task RevokeAllAccessTokenByUser(int userId, string reason);

    public Task RevokeAllRefreshTokenByUser(int userId, string reason);

    public Task<IEnumerable<SessionDto>> GetTokenSessionsByUserId(int userId, int limit, int offset, bool showRevoked = false, bool showExpired = false);

    public Task<int> GetTokenSessionsCountByUserId(int userId, bool showRevoked = false, bool showExpired = false);

    public Task<SessionDto> GetTokenSessionById(string id, int userId, bool showRevoked = false, bool showExpired = false);

    public Task<Guid> GetSessionIdByTokenId(string id, int userId);

    public Task<SessionDto> RevokeSessionById(string id, string reason, int userId);

    public Task RevokePairTokenByRefreshToken(string refreshToken, string reason, int? userId = null);
}