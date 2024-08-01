using electrostore.Dto;

namespace electrostore.Services.UserService;

public interface IUserService
{
    public Task<IEnumerable<ReadUserDto>> GetUsers(int limit = 100, int offset = 0);

    public Task<ReadUserDto> CreateUser(CreateUserDto userDto);

    public Task<ReadUserDto> GetUserById(int id);

    public Task<ReadUserDto> GetUserByEmail(string email);

    public Task<ReadUserDto> UpdateUser(int id, UpdateUserDto userDto);

    public Task DeleteUser(int id);

    public Task<bool> CheckUserPassword(string email, string password);
}