using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.JwiService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/user/{id_user}/sessions")]

    public class UserTokenController : ControllerBase
    {
        private readonly IJwiService _jwiService;

        public UserTokenController(IJwiService jwiService)
        {
            _jwiService = jwiService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadRefreshTokenDto>>> GetSessions([FromRoute] int id_user, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
            [FromQuery] bool showRevoked = false, [FromQuery] bool showExpired = false)
        {
            var sessions = await _jwiService.GetTokenSessionsByUserId(id_user, limit, offset, showRevoked, showExpired);
            var CountList = await _jwiService.GetTokenSessionsCountByUserId(id_user);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers","X-Total-Count");
            return Ok(sessions);
        }

        [HttpGet("{session_id}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadRefreshTokenDto>> GetSessionById([FromRoute] int id_user, [FromRoute] string session_id,
            [FromQuery] bool showRevoked = false, [FromQuery] bool showExpired = false)
        {
            var session = await _jwiService.GetTokenSessionById(session_id, id_user, showRevoked, showExpired);
            return Ok(session);
        }

        [HttpPut("{session_id}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> RevokeSessionById([FromRoute] int id_user, [FromRoute] string session_id, [FromBody] UpdateAccessTokenDto updateAccessTokenDto)
        {
            await _jwiService.RevokeSessionById(session_id, updateAccessTokenDto.revoked_reason ?? "Revoked by user", id_user);
            return NoContent();
        }
    }
}