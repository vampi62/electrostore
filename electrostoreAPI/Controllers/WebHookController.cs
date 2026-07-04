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
        public async Task Post17Track([FromBody] string body)
        {
            Console.WriteLine("Received webhook headers:");
            Console.WriteLine(Request.Headers.ToString());
            Console.WriteLine("Received webhook from 17Track:");
            Console.WriteLine(body);
            var result = await _webHookService.Process17TrackWebhook(body);
            if (!result)
            {
                throw new Exception("Failed to process webhook");
            }
            Ok();
        }
    }
}