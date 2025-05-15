using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ConfigService;

namespace electrostore.Controllers
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