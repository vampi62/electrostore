using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.ZoneService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/zone")]
    public class ZoneController : ControllerBase
    {
        private readonly IZoneService _zoneService;
        private readonly IFileService _fileService;

        public ZoneController(IZoneService zoneService, IFileService fileService)
        {
            _zoneService = zoneService;
            _fileService = fileService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedZoneDto>>> GetZones([FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'stores'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to select list of ID to research in the base. Multiple values can be specified by separating them with ','.")] List<int>? idResearch = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'name_zone=like=example'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'name_zone,asc' or 'name_zone,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var zones = await _zoneService.GetZones(limit, offset, rsqlDto, sortDto, expand, idResearch);
            return Ok(zones);
        }

        [HttpGet("{id_zone}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedZoneDto>> GetZoneById([FromRoute] int id_zone,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'stores'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var zone = await _zoneService.GetZoneById(id_zone, expand);
            return Ok(zone);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadZoneDto>> CreateZone([FromBody] CreateZoneDto zone)
        {
            var newZone = await _zoneService.CreateZone(zone);
            return CreatedAtAction(nameof(GetZoneById), new { id_zone = newZone.id_zone }, newZone);
        }

        [HttpPut("{id_zone}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadZoneDto>> UpdateZone([FromRoute] int id_zone, [FromBody] UpdateZoneDto zone)
        {
            var zoneToUpdate = await _zoneService.UpdateZone(id_zone, zone);
            return Ok(zoneToUpdate);
        }

        [HttpDelete("{id_zone}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteZone([FromRoute] int id_zone)
        {
            await _zoneService.DeleteZone(id_zone);
            return NoContent();
        }

        [HttpPost("{id_zone}/picture")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadZoneDto>> UploadZonePicture([FromRoute] int id_zone, [FromForm] CreateZonePictureDto picture)
        {
            var zone = await _zoneService.UploadZonePicture(id_zone, picture.img_file);
            return Ok(zone);
        }

        [HttpDelete("{id_zone}/picture")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadZoneDto>> DeleteZonePicture([FromRoute] int id_zone)
        {
            var zone = await _zoneService.DeleteZonePicture(id_zone);
            return Ok(zone);
        }

        [HttpGet("{id_zone}/picture")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> GetZonePicture([FromRoute] int id_zone)
        {
            var zone = await _zoneService.GetZoneById(id_zone);
            if (zone.url_picture_zone is null)
            {
                return NotFound("This zone has no picture");
            }
            var result = await _fileService.GetFile(zone.url_picture_zone);
            if (result.Success && result.FileStream != null)
            {
                return File(result.FileStream, result.MimeType);
            }
            return NotFound(result.ErrorMessage);
        }

        [HttpGet("{id_zone}/thumbnail")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> GetZoneThumbnail([FromRoute] int id_zone)
        {
            var zone = await _zoneService.GetZoneById(id_zone);
            if (zone.url_thumbnail_zone is null)
            {
                return NotFound("This zone has no picture");
            }
            var result = await _fileService.GetFile(zone.url_thumbnail_zone);
            if (result.Success && result.FileStream != null)
            {
                return File(result.FileStream, result.MimeType);
            }
            return NotFound(result.ErrorMessage);
        }
    }
}
