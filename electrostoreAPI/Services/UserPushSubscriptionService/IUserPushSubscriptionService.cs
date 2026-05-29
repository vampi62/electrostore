using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.UserPushSubscriptionService;

public interface IUserPushSubscriptionService
{
    Task<PaginatedResponseDto<ReadUserPushSubscriptionDto>> GetPushSubscriptionsByUserId(int userId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);
    Task<ReadUserPushSubscriptionDto> GetPushSubscriptionById(int id, int? userId = null);
    Task<ReadUserPushSubscriptionDto> CreatePushSubscription(CreateUserPushSubscriptionDto dto);
    Task DeletePushSubscription(int id, int? userId = null);
}
