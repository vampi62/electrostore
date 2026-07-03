using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.CarrierService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/carrier")]
    public class CarrierController : ControllerBase
    {
        private readonly ICarrierService _carrierService;

        public CarrierController(ICarrierService carrierService)
        {
            _carrierService = carrierService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadCarrierDto>>> GetCarriers([FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to select list of ID to research in the base. Multiple values can be specified by separating them with ','.")] List<int>? idResearch = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'nom_carrier=like=example'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'nom_carrier,asc' or 'nom_carrier,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var carriers = await _carrierService.GetCarriers(limit, offset, rsqlDto, sortDto, idResearch);
            return Ok(carriers);
        }

        [HttpGet("{id_carrier}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCarrierDto>> GetCarrierById([FromRoute] int id_carrier)
        {
            var carrier = await _carrierService.GetCarrierById(id_carrier);
            return Ok(carrier);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCarrierDto>> CreateCarrier([FromBody] CreateCarrierDto carrier)
        {
            var newCarrier = await _carrierService.CreateCarrier(carrier);
            return CreatedAtAction(nameof(GetCarrierById), new { id_carrier = newCarrier.id_carrier }, newCarrier);
        }

        [HttpPut("{id_carrier}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCarrierDto>> UpdateCarrier([FromRoute] int id_carrier, [FromBody] UpdateCarrierDto carrier)
        {
            var carrierToUpdate = await _carrierService.UpdateCarrier(id_carrier, carrier);
            return Ok(carrierToUpdate);
        }

        [HttpDelete("{id_carrier}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteCarrier([FromRoute] int id_carrier)
        {
            await _carrierService.DeleteCarrier(id_carrier);
            return NoContent();
        }
    }
}