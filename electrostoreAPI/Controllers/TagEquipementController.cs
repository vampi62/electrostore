using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.EquipementTagService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/tag/{id_tag}/equipement")]

    public class TagEquipementController : ControllerBase
    {
        private readonly IEquipementTagService _equipementTagService;

        public TagEquipementController(IEquipementTagService equipementTagService)
        {
            _equipementTagService = equipementTagService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedEquipementTagDto>>> GetEquipementsTagsByTagId([FromRoute] int id_tag, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'tag', 'equipement'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'id_equipement==1'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'id_equipement,asc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var equipementTags = await _equipementTagService.GetEquipementsTagsByTagId(id_tag, limit, offset, rsqlDto, sortDto, expand);
            return Ok(equipementTags);
        }

        [HttpGet("{id_equipement}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedEquipementTagDto>> GetEquipementTagById([FromRoute] int id_tag, [FromRoute] int id_equipement,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'tag', 'equipement'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var equipementTag = await _equipementTagService.GetEquipementTagById(id_equipement, id_tag, expand);
            return Ok(equipementTag);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadEquipementTagDto>> CreateEquipementTag([FromRoute] int id_tag, [FromBody] CreateEquipementTagByTagDto equipementTagDto)
        {
            var equipementTagDtoFull = new CreateEquipementTagDto
            {
                id_tag = id_tag,
                id_equipement = equipementTagDto.id_equipement
            };
            var equipementTag = await _equipementTagService.CreateEquipementTag(equipementTagDtoFull);
            return CreatedAtAction(nameof(GetEquipementTagById), new { id_tag = equipementTag.id_tag, id_equipement = equipementTag.id_equipement }, equipementTag);
        }

        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkEquipementTagDto>> CreateBulkEquipementTag([FromRoute] int id_tag, [FromBody] List<CreateEquipementTagByTagDto> equipementTagsDto)
        {
            var equipementTagsDtoFull = equipementTagsDto.Select(equipementTagDto => new CreateEquipementTagDto
            {
                id_tag = id_tag,
                id_equipement = equipementTagDto.id_equipement
            }).ToList();
            var equipementTags = await _equipementTagService.CreateBulkEquipementTag(equipementTagsDtoFull);
            return Ok(equipementTags);
        }

        [HttpDelete("{id_equipement}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteEquipementTag([FromRoute] int id_tag, [FromRoute] int id_equipement)
        {
            await _equipementTagService.DeleteEquipementTag(id_equipement, id_tag);
            return NoContent();
        }

        [HttpDelete("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkEquipementTagDto>> DeleteBulkEquipementTag([FromRoute] int id_tag, [FromBody] List<int> id_equipements)
        {
            var equipementTagsDtoFull = id_equipements.Select(id_equipement => new CreateEquipementTagDto
            {
                id_equipement = id_equipement,
                id_tag = id_tag
            }).ToList();
            var equipementTags = await _equipementTagService.DeleteBulkEquipementTag(equipementTagsDtoFull);
            return Ok(equipementTags);
        }
    }
}
