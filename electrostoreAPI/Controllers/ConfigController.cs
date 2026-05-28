using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Services.ConfigService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/config")]
    public class ConfigController : ControllerBase
    {
        private readonly IConfigService _configService;

        public ConfigController(IConfigService configService)
        {
            _configService = configService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ReadConfig>> GetConfigs()
        {
            var configs = await _configService.getAllConfig();
            return Ok(configs);
        }
    }
}