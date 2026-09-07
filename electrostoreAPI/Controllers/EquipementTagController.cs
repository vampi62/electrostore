using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.EquipementTagService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/equipement/{id_equipement}/tag")]

    public class EquipementTagController : ControllerBase
    {
        private readonly IEquipementTagService _equipementTagService;

        public EquipementTagController(IEquipementTagService equipementTagService)
        {
            _equipementTagService = equipementTagService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedEquipementTagDto>>> GetEquipementsTagsByEquipementId([FromRoute] int id_equipement, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'tag', 'equipement'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL filter. Example: 'id_tag==5'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'id_tag,asc' or 'id_tag,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var equipementTags = await _equipementTagService.GetEquipementsTagsByEquipementId(id_equipement, limit, offset, rsqlDto, sortDto, expand);
            return Ok(equipementTags);
        }

        [HttpGet("{id_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedEquipementTagDto>> GetEquipementTagById([FromRoute] int id_equipement, [FromRoute] int id_tag,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'tag', 'equipement'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var equipementTag = await _equipementTagService.GetEquipementTagById(id_equipement, id_tag, expand);
            return Ok(equipementTag);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadEquipementTagDto>> CreateEquipementTag([FromRoute] int id_equipement, [FromBody] CreateEquipementTagByEquipementDto equipementTagDto)
        {
            var equipementTagDtoFull = new CreateEquipementTagDto
            {
                id_equipement = id_equipement,
                id_tag = equipementTagDto.id_tag
            };
            var equipementTag = await _equipementTagService.CreateEquipementTag(equipementTagDtoFull);
            return CreatedAtAction(nameof(GetEquipementTagById), new { id_equipement = equipementTag.id_equipement, id_tag = equipementTag.id_tag }, equipementTag);
        }

        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkEquipementTagDto>> CreateBulkEquipementTag([FromRoute] int id_equipement, [FromBody] List<CreateEquipementTagByEquipementDto> equipementTagsDto)
        {
            var equipementTagsDtoFull = equipementTagsDto.Select(equipementTagDto => new CreateEquipementTagDto
            {
                id_equipement = id_equipement,
                id_tag = equipementTagDto.id_tag
            }).ToList();
            var equipementTags = await _equipementTagService.CreateBulkEquipementTag(equipementTagsDtoFull);
            return Ok(equipementTags);
        }

        [HttpDelete("{id_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteEquipementTag([FromRoute] int id_equipement, [FromRoute] int id_tag)
        {
            await _equipementTagService.DeleteEquipementTag(id_equipement, id_tag);
            return NoContent();
        }

        [HttpDelete("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkEquipementTagDto>> DeleteBulkEquipementTag([FromRoute] int id_equipement, [FromBody] List<int> id_tags)
        {
            var equipementTagsDtoFull = id_tags.Select(id_tag => new CreateEquipementTagDto
            {
                id_equipement = id_equipement,
                id_tag = id_tag
            }).ToList();
            var equipementTags = await _equipementTagService.DeleteBulkEquipementTag(equipementTagsDtoFull);
            return Ok(equipementTags);
        }
    }
}
