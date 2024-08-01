using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.UserService;

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
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<ReadUserDto>> CreateUser([FromBody] CreateUserDto userDto)
        {
            var user = await _userService.CreateUser(userDto);
            return CreatedAtAction(nameof(GetUserById), new { id_user = user.id_user }, user);
        }

        [HttpPut("{id_user}")]
        public async Task<ActionResult<ReadUserDto>> UpdateUser([FromRoute] int id_user, [FromBody] UpdateUserDto userDto)
        {
            var user = await _userService.UpdateUser(id_user, userDto);
            return Ok(user);
        }

        [HttpDelete("{id_user}")]
        public async Task<ActionResult> DeleteUser([FromRoute] int id_user)
        {
            await _userService.DeleteUser(id_user);
            return NoContent();
        }
    }
}