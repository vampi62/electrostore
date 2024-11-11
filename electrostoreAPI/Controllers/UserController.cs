using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.UserService;
using electrostore.Services.JwtService;
using electrostore.Services.JwiService;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/user")]

    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly JwtService _jwtService;
        private readonly IJwiService _jwiService;

        public UserController(IUserService userService, JwtService jwtService, IJwiService jwiService)
        {
            _userService = userService;
            _jwtService = jwtService;
            _jwiService = jwiService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadUserDto>>> GetUsers([FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var users = await _userService.GetUsers(limit, offset);
            return Ok(users);
        }

        [HttpGet("{id_user}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadUserDto>> GetUserById([FromRoute] int id_user)
        {
            var user = await _userService.GetUserById(id_user);
            return Ok(user);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<ReadUserDto>> CreateUser([FromBody] CreateUserDto userDto)
        {
            //if a bearer token is provided, the user is already authenticated and if the user is an admin, he can create a user with any role
            // if the user is not an admin, he can only create a user with the role "user"
            if (User != null)
            {
                if (!User.IsInRole("admin") && userDto.role_user != "user")
                {
                    return Unauthorized(new { message = "You are not allowed to create a user with this role" });
                }
            }
            var user = await _userService.CreateUser(userDto);
            return CreatedAtAction(nameof(GetUserById), new { id_user = user.id_user }, user);
        }

        [HttpPut("{id_user}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadUserDto>> UpdateUser([FromRoute] int id_user, [FromBody] UpdateUserDto userDto)
        {
            if (!User.IsInRole("admin") && id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to update this user" });
            }
            var user = await _userService.UpdateUser(id_user, userDto);
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            // Revoke all access token for this user
            await _jwiService.RevokeAllAccessTokenByUser(id_user, clientIp, "User update information");
            return Ok(user);
        }

        [HttpDelete("{id_user}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteUser([FromRoute] int id_user)
        {
            if (!User.IsInRole("admin") && id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to delete this user" });
            }
            await _userService.DeleteUser(id_user);
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            // Revoke all access token for this user
            await _jwiService.RevokeAllAccessTokenByUser(id_user, clientIp, "User delete account");
            await _jwiService.RevokeAllRefreshTokenByUser(id_user, clientIp, "User delete account");
            return NoContent();
        }
    
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest loginRequest)
        {
            if (!await _userService.CheckUserPassword(loginRequest.Email, loginRequest.Password))
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }
            var user = await _userService.GetUserByEmail(loginRequest.Email);
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var token = _jwtService.GenerateToken(user);

            // save token in database
            await _jwiService.SaveToken(token, user.id_user, clientIp);
            return Ok(new LoginResponse
            {
                token = token.token,
                expire_date_token = token.expire_date_token.ToString("yyyy-MM-dd HH:mm:ss"),
                refesh_token = token.refesh_token,
                expire_date_refresh_token = token.expire_date_refresh_token.ToString("yyyy-MM-dd HH:mm:ss"),
                user = user
            });
        }

        [HttpPost("refresh-token")]
        [Authorize(Policy = "RefreshToken")]
        public async Task<ActionResult<LoginResponse>> RefreshToken()
        {
            var user = await _userService.GetUserById(int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""));
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var token = _jwtService.GenerateToken(user);
            // save token in database
            await _jwiService.SaveToken(token, user.id_user, clientIp);
            return Ok(new LoginResponse
            {
                token = token.token,
                expire_date_token = token.expire_date_token.ToString("yyyy-MM-dd HH:mm:ss"),
                refesh_token = token.refesh_token,
                expire_date_refresh_token = token.expire_date_refresh_token.ToString("yyyy-MM-dd HH:mm:ss"),
                user = user
            });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest forgotPasswordRequest)
        {
            await _userService.ForgotPassword(forgotPasswordRequest);
            return Ok();
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest resetPasswordRequest)
        {
            var user = await _userService.ResetPassword(resetPasswordRequest);
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            // Revoke all access token for this user
            await _jwiService.RevokeAllAccessTokenByUser(user.id_user, clientIp, "User reset password");
            await _jwiService.RevokeAllRefreshTokenByUser(user.id_user, clientIp, "User reset password");
            return Ok();
        }
    }
}