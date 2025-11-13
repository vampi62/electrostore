using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ProjetStatusService;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/projet/{id_projet}/status-history")]

    public class ProjetStatusController : ControllerBase
    {
        private readonly IProjetStatusService _projetStatusService;

        public ProjetStatusController(IProjetStatusService projetStatusService)
        {
            _projetStatusService = projetStatusService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadExtendedProjetStatusDto>>> GetProjetStatusByProjetId([FromRoute] int id_projet, [FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetStatus = await _projetStatusService.GetProjetStatusByProjetId(id_projet, limit, offset, expand);
            var CountList = await _projetStatusService.GetProjetStatusCountByProjetId(id_projet);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");
            return Ok(projetStatus);
        }

        [HttpGet("{id_projet_status}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjetStatusDto>> GetProjetStatusById([FromRoute] int id_projet, [FromRoute] int id_projet_status, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetStatus = await _projetStatusService.GetProjetStatusById(id_projet_status, null, id_projet, expand);
            return Ok(projetStatus);
        }
    }
}