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
        public async Task<ActionResult<IEnumerable<SessionDto>>> GetSessions([FromRoute] int id_user, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
            [FromQuery] bool show_revoked = false, [FromQuery] bool show_expired = false)
        {
            var sessions = await _jwiService.GetTokenSessionsByUserId(id_user, limit, offset, show_revoked, show_expired);
            var CountList = await _jwiService.GetTokenSessionsCountByUserId(id_user, show_revoked, show_expired);
            Response.Headers["X-Total-Count"] = CountList.ToString();
            Response.Headers.AccessControlExposeHeaders = "X-Total-Count";
            return Ok(sessions);
        }

        [HttpGet("{session_id}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<SessionDto>> GetSessionById([FromRoute] int id_user, [FromRoute] string session_id,
            [FromQuery] bool show_revoked = false, [FromQuery] bool show_expired = false)
        {
            var session = await _jwiService.GetTokenSessionById(session_id, id_user, show_revoked, show_expired);
            return Ok(session);
        }

        [HttpPut("{session_id}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<SessionDto>> RevokeSessionById([FromRoute] int id_user, [FromRoute] string session_id, [FromBody] UpdateAccessTokenDto updateAccessTokenDto)
        {
            var session = await _jwiService.RevokeSessionById(session_id, updateAccessTokenDto.revoked_reason ?? "Revoked by user", id_user);
            return Ok(session);
        }
    }
}