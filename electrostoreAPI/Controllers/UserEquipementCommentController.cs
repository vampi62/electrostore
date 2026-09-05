using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.EquipementCommentService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/user/{id_user}/equipement_comment")]

    public class UserEquipementCommentController : ControllerBase
    {
        private readonly IEquipementCommentService _equipementCommentService;

        public UserEquipementCommentController(IEquipementCommentService equipementCommentService)
        {
            _equipementCommentService = equipementCommentService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedEquipementCommentDto>>> GetEquipementsCommentsByUserId([FromRoute] int id_user, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'equipement', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'content_equipement_comment=like=example'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'created_at,asc' or 'created_at,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var equipementComments = await _equipementCommentService.GetEquipementsCommentsByUserId(id_user, limit, offset, rsqlDto, sortDto, expand);
            return Ok(equipementComments);
        }

        [HttpGet("{id_equipement_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedEquipementCommentDto>> GetEquipementsCommentById([FromRoute] int id_user, [FromRoute] int id_equipement_comment,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'equipement', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var equipementComment = await _equipementCommentService.GetEquipementCommentById(id_equipement_comment, id_user, null, expand);
            return Ok(equipementComment);
        }

        // no create equipement comment in user controller

        [HttpPut("{id_equipement_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadEquipementCommentDto>> UpdateEquipementComment([FromRoute] int id_user, [FromRoute] int id_equipement_comment, [FromBody] UpdateEquipementCommentDto equipementCommentDto)
        {
            var equipementComment = await _equipementCommentService.UpdateComment(id_equipement_comment, equipementCommentDto, id_user);
            return Ok(equipementComment);
        }

        [HttpDelete("{id_equipement_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteEquipementComment([FromRoute] int id_user, [FromRoute] int id_equipement_comment)
        {
            await _equipementCommentService.DeleteComment(id_equipement_comment, id_user);
            return NoContent();
        }
    }
}
