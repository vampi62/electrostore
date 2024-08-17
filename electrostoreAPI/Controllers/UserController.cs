using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.UserService;
using OneOf.Types;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/user")]

    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
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
        public async Task<ActionResult<ReadUserDto>> CreateUser([FromBody] CreateUserDto userDto)
        {
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
            var result = await _userService.DeleteUser(id_user);
            if (result is BadRequestObjectResult)
            {
                return result;
            }
            return NoContent();
        }
    }
}