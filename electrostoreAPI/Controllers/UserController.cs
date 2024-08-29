using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.UserService;
using electrostore.Services.JwtService;
using System.Security.Claims;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/user")]

    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly JwtService _jwtService;

        public UserController(IUserService userService, JwtService jwtService)
        {
            _userService = userService;
            _jwtService = jwtService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadUserDto>>> GetUsers([FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var users = await _userService.GetUsers(limit, offset);
            return Ok(users);
        }

        [HttpGet("{id_user}")]
        public async Task<ActionResult<ReadUserDto>> GetUserById([FromRoute] int id_user)
        {
            var user = await _userService.GetUserById(id_user);
            if (user.Result is BadRequestObjectResult)
            {
                return user.Result;
            }
            if (user.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(user.Value);
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
            if (user.Result is BadRequestObjectResult)
            {
                return user.Result;
            }
            if (user.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetUserById), new { id_user = user.Value.id_user }, user.Value);
        }

        [HttpPut("{id_user}")]
        public async Task<ActionResult<ReadUserDto>> UpdateUser([FromRoute] int id_user, [FromBody] UpdateUserDto userDto)
        {
            if (!User.IsInRole("admin") && id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to update this user" });
            }

            var user = await _userService.UpdateUser(id_user, userDto);
            if (user.Result is BadRequestObjectResult)
            {
                return user.Result;
            }
            if (user.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(user.Value);
        }

        [HttpDelete("{id_user}")]
        public async Task<ActionResult> DeleteUser([FromRoute] int id_user)
        {
            if (!User.IsInRole("admin") && id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to delete this user" });
            }
            var result = await _userService.DeleteUser(id_user);
            if (result is BadRequestObjectResult)
            {
                return result;
            }
            return NoContent();
        }
    
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            var user = await _userService.GetUserByEmail(loginRequest.Email);
            if (user.Result is BadRequestObjectResult)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }
            if (!await _userService.CheckUserPassword(loginRequest.Email, loginRequest.Password))
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }
            if (user.Value == null)
            {
                return StatusCode(500);
            }

            var token = _jwtService.GenerateToken(user.Value);
            return Ok(new { Token = token });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest forgotPasswordRequest)
        {
            var result = await _userService.ForgotPassword(forgotPasswordRequest);
            if (result is BadRequestObjectResult)
            {
                return result;
            }
            if (result == null)
            {
                return StatusCode(500);
            }
            return Ok();
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest resetPasswordRequest)
        {
            var result = await _userService.ResetPassword(resetPasswordRequest);
            if (result is BadRequestObjectResult)
            {
                return result;
            }
            if (result == null)
            {
                return StatusCode(500);
            }
            return Ok();
        }
    }
}