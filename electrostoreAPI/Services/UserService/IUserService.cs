using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.UserService;

public interface IUserService
{
    public Task<IEnumerable<ReadExtendedUserDto>> GetUsers(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null);

    public Task<int> GetUsersCount();

    public Task<ReadUserDto> CreateUser(CreateUserDto userDto);

    public Task<ReadExtendedUserDto> GetUserById(int id, List<string>? expand = null);

    public Task<ReadUserDto> GetUserByEmail(string email);

    public Task<ReadUserDto> UpdateUser(int id, UpdateUserDto userDto);

    public Task DeleteUser(int id);

    public Task<bool> CheckUserPasswordByEmail(string email, string password);

    public Task<bool> CheckUserPasswordById(int id, string password);

    public Task ForgotPassword(ForgotPasswordRequest request);

    public Task<ReadUserDto> ResetPassword(ResetPasswordRequest request);
}