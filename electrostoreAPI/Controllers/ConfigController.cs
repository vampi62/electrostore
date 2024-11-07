using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Services.JwtService;
using electrostore.Dto;
using MQTTnet.Client;
using System.Net.NetworkInformation;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/config")]
    public class ConfigController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IMqttClient _mqttClient;

        public ConfigController(IConfiguration configuration, IMqttClient mqttClient)
        {
            _configuration = configuration;
            _mqttClient = mqttClient;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ReadConfig>> GetConfigs()
        {
            using var ping = new Ping();
            // ping juste one time in < 0.3s
            var reply = await ping.SendPingAsync("electrostoreia", 300);
            return Ok(new ReadConfig {
                // get if the smtp is enabled
                smtp_enabled = _configuration["SMTP:Enable"] == "true",
                // check if the mqtt is connected
                mqtt_connected = _mqttClient.IsConnected,
                // ping the iaElectrostoreAPI
                ia_connected = reply.Status == IPStatus.Success
            });
        }
    }
}