using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.JwiService;
using Swashbuckle.AspNetCore.Annotations;
using electrostore.Extensions;

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
        public async Task<ActionResult<PaginatedResponseDto<SessionDto>>> GetSessions([FromRoute] int id_user, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'is_revoked==true' or 'is_revoked==false'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'created_at,asc' or 'created_at,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var sessions = await _jwiService.GetTokenSessionsByUserId(id_user, limit, offset, rsqlDto, sortDto);
            return Ok(sessions);
        }

        [HttpGet("{session_id}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<SessionDto>> GetSessionById([FromRoute] int id_user, [FromRoute] string session_id)
        {
            var session = await _jwiService.GetTokenSessionById(session_id, id_user);
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