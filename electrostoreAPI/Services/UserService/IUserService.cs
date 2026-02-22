using electrostore.Dto;

namespace electrostore.Services.UserService;

public interface IUserService
{
    public Task<PaginatedResponseDto<ReadExtendedUserDto>> GetUsers(int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null, List<int>? idResearch = null);

    public Task<ReadUserDto> CreateUser(CreateUserDto userDto);
    
    public Task<ReadUserDto> CreateFirstAdminUser(CreateUserDto userDto);

    public Task<ReadExtendedUserDto> GetUserById(int id, List<string>? expand = null);

    public Task<ReadUserDto> UpdateUser(int id, UpdateUserDto userDto);

    public Task DeleteUser(int id);
}