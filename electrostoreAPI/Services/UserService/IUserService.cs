using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.UserService;

public interface IUserService
{
    public Task<IEnumerable<ReadUserDto>> GetUsers(int limit = 100, int offset = 0);

    public Task<ActionResult<ReadUserDto>> CreateUser(CreateUserDto userDto);

    public Task<ActionResult<ReadUserDto>> GetUserById(int id);

    public Task<ActionResult<ReadUserDto>> GetUserByEmail(string email);

    public Task<ActionResult<ReadUserDto>> UpdateUser(int id, UpdateUserDto userDto);

    public Task<ActionResult> DeleteUser(int id);

    public Task<bool> CheckUserPassword(string email, string password);

    public Task<ActionResult> ForgotPassword(ForgotPasswordRequest request);

    public Task<ActionResult> ResetPassword(ResetPasswordRequest request);
}