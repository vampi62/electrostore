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
            PingReply reply;
            // ping juste one time in < 0.3s
            try {
                reply = await ping.SendPingAsync("electrostoreIA", 300);
            }
            catch (PingException) {
                reply = null;
            }
            return Ok(new ReadConfig {
                // get if the smtp is enabled
                smtp_enabled = _configuration["SMTP:Enable"] == "true",
                // check if the mqtt is connected
                mqtt_connected = _mqttClient.IsConnected,
                // ping the iaElectrostoreAPI
                ia_connected = reply?.Status == IPStatus.Success,
                // get the max length of the url
                max_length_url = Constants.MaxUrlLength,
                // get the max length
                max_length_commentaire = Constants.MaxCommentaireLength,
                max_length_description = Constants.MaxDescriptionLength,
                max_length_name = Constants.MaxNameLength,
                max_length_type = Constants.MaxTypeLength,
                max_length_email = Constants.MaxEmailLength,
                max_length_ip = Constants.MaxIpLength,
                max_length_reason = Constants.MaxReasonLength,
                max_length_role = Constants.MaxRoleLength,
                max_length_status = Constants.MaxStatusLength,
                max_size_document_in_mb = Constants.MaxDocumentSizeMB
            });
        }
    }
}