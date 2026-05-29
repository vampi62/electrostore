using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Services.StatusService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/status")]
    public class StatusController : ControllerBase
    {
        private readonly IStatusService _statusService;

        public StatusController(IStatusService statusService)
        {
            _statusService = statusService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ReadStatusDto>> GetStatus()
        {
            var status = await _statusService.GetStatus();
            return Ok(status);
        }
    }
}
