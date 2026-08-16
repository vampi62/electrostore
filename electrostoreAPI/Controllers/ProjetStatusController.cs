using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ProjetStatusService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/projet/{id_project}/status-history")]

    public class ProjetStatusController : ControllerBase
    {
        private readonly IProjetStatusService _projetStatusService;

        public ProjetStatusController(IProjetStatusService projetStatusService)
        {
            _projetStatusService = projetStatusService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjetStatusDto>>> GetProjetStatusByProjetId([FromRoute] int id_project, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL filter. Example: 'status_project==0'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'created_at,asc' or 'created_at,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projetStatus = await _projetStatusService.GetProjetStatusByProjetId(id_project, limit, offset, rsqlDto, sortDto);
            return Ok(projetStatus);
        }

        [HttpGet("{id_project_status}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjetStatusDto>> GetProjetStatusById([FromRoute] int id_project, [FromRoute] int id_project_status)
        {
            var projetStatus = await _projetStatusService.GetProjetStatusById(id_project_status, id_project);
            return Ok(projetStatus);
        }
    }
}