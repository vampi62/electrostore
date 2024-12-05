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
            if (!User.IsInRole("admin") && id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to view this user's tokens" });
            }
            var tokens = await _jwiService.GetRefreshTokensByUserId(id_user, limit, offset);
            var CountList = await _jwiService.GetRefreshTokensCountByUserId(id_user);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers","X-Total-Count");
            return Ok(tokens);
        }

        [HttpPut("{id_token}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> RevokeAccessTokenById([FromRoute] int id_user, [FromRoute] string id_token, [FromBody] UpdateAccessTokenDto updateAccessTokenDto)
        {
            if (!User.IsInRole("admin") && id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to revoke this token" });
            }
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            await _jwiService.RevokePairTokenByRefreshToken(id_token, clientIp, updateAccessTokenDto.revoked_reason ?? "Revoked by user", id_user);
            return NoContent();
        }
    }
}