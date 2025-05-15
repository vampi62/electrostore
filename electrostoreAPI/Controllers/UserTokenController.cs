using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.JwiService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/user/{id_user}/token")]

    public class UserTokenController : ControllerBase
    {
        private readonly IJwiService _jwiService;

        public UserTokenController(IJwiService jwiService)
        {
            _jwiService = jwiService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadRefreshTokenDto>>> GetAccessTokens([FromRoute] int id_user, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var tokens = await _jwiService.GetRefreshTokensByUserId(id_user, limit, offset);
            var CountList = await _jwiService.GetRefreshTokensCountByUserId(id_user);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers","X-Total-Count");
            return Ok(tokens);
        }

        [HttpGet("{id_token}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadRefreshTokenDto>> GetAccessTokenById([FromRoute] int id_user, [FromRoute] string id_token)
        {
            var token = await _jwiService.GetRefreshTokenByToken(id_user, id_token);
            return Ok(token);
        }

        [HttpPut("{id_token}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> RevokeAccessTokenById([FromRoute] int id_user, [FromRoute] string id_token, [FromBody] UpdateAccessTokenDto updateAccessTokenDto)
        {
            await _jwiService.RevokePairTokenByRefreshToken(id_token, updateAccessTokenDto.revoked_reason ?? "Revoked by user", id_user);
            return NoContent();
        }
    }
}