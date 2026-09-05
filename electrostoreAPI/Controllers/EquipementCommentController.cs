using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.EquipementCommentService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/equipement/{id_equipement}/comment")]

    public class EquipementCommentController : ControllerBase
    {
        private readonly IEquipementCommentService _equipementCommentService;

        public EquipementCommentController(IEquipementCommentService equipementCommentService)
        {
            _equipementCommentService = equipementCommentService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedEquipementCommentDto>>> GetEquipementsCommentsByEquipementId([FromRoute] int id_equipement, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'equipement', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'content_equipement_comment=like=example'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'created_at,asc' or 'created_at,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var equipementComments = await _equipementCommentService.GetEquipementsCommentsByEquipementId(id_equipement, limit, offset, rsqlDto, sortDto, expand);
            return Ok(equipementComments);
        }

        [HttpGet("{id_equipement_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedEquipementCommentDto>> GetEquipementsCommentById([FromRoute] int id_equipement, [FromRoute] int id_equipement_comment,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'equipement', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var equipementComment = await _equipementCommentService.GetEquipementCommentById(id_equipement_comment, null, id_equipement, expand);
            return Ok(equipementComment);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadEquipementCommentDto>> CreateComment([FromRoute] int id_equipement, [FromBody] CreateEquipementCommentByEquipementDto equipementCommentDto)
        {
            var equipementCommentDtoFull = new CreateEquipementCommentDto
            {
                id_equipement = id_equipement,
                id_user = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : throw new InvalidOperationException("User identifier not found."),
                content_equipement_comment = equipementCommentDto.content_equipement_comment
            };
            var equipementComment = await _equipementCommentService.CreateComment(equipementCommentDtoFull);
            return CreatedAtAction(nameof(GetEquipementsCommentById), new { id_equipement = equipementComment.id_equipement, id_equipement_comment = equipementComment.id_equipement_comment }, equipementComment);
        }

        [HttpPut("{id_equipement_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadEquipementCommentDto>> UpdateComment([FromRoute] int id_equipement, [FromRoute] int id_equipement_comment, [FromBody] UpdateEquipementCommentDto equipementCommentDto)
        {
            var equipementComment = await _equipementCommentService.UpdateComment(id_equipement_comment, equipementCommentDto, null, id_equipement);
            return Ok(equipementComment);
        }

        [HttpDelete("{id_equipement_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteComment([FromRoute] int id_equipement, [FromRoute] int id_equipement_comment)
        {
            await _equipementCommentService.DeleteComment(id_equipement_comment, null, id_equipement);
            return NoContent();
        }
    }
}
