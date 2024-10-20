using Microsoft.AspNetCore.Mvc;
using electrostore.Services.JwtService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/config")]
    public class ConfigController : ControllerBase
    {
        private readonly JwtService _jwtService;
        private readonly IConfiguration _configuration;

        public ConfigController(JwtService jwtService)
        {
            _jwtService = jwtService;
        }

        [HttpGet]
        public async Task<ActionResult> GetConfigs()
        {
            // return if SMTP is enabled
            return Ok(new { smtp = _configuration["SMTP:Enable"] });
        }
    }
}