using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.UserService;
using Swashbuckle.AspNetCore.Annotations;

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
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadExtendedUserDto>>> GetUsers([FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projets_commentaires', 'commands_commentaires'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to select list of ID to research in the base. Multiple values can be specified by separating them with ','.")] List<int>? idResearch = null)
        {
            var users = await _userService.GetUsers(limit, offset, expand, idResearch);
            var CountList = await _userService.GetUsersCount();
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers","X-Total-Count");
            return Ok(users);
        }

        [HttpGet("{id_user}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedUserDto>> GetUserById([FromRoute] int id_user, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projets_commentaires', 'commands_commentaires'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var user = await _userService.GetUserById(id_user, expand);
            return Ok(user);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<ReadUserDto>> CreateUser([FromBody] CreateUserDto userDto)
        {

            var user = await _userService.CreateUser(userDto);
            return CreatedAtAction(nameof(GetUserById), new { id_user = user.id_user }, user);
        }

        [HttpPut("{id_user}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadUserDto>> UpdateUser([FromRoute] int id_user, [FromBody] UpdateUserDto userDto)
        {
            var user = await _userService.UpdateUser(id_user, userDto);
            return Ok(user);
        }

        [HttpDelete("{id_user}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteUser([FromRoute] int id_user)
        {
            await _userService.DeleteUser(id_user);
            return NoContent();
        }
    }
}