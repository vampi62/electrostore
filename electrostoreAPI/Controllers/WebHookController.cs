using System.Text.Json;
using ElectrostoreAPI.Services.WebHookService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/webhook/17track")]

    public class WebHookController : ControllerBase
    {
        private readonly IWebHookService _webHookService;

        public WebHookController(IWebHookService webHookService)
        {
            _webHookService = webHookService;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task Post17Track([FromBody] JsonElement body)
        {
            if (!Request.Headers.TryGetValue("Sign", out var signatureHeader))
            {
                throw new ArgumentException("Missing signature header");
            }
            await _webHookService.Process17TrackWebhook(body, signatureHeader.ToString());
            Ok();
        }
    }
}