using MQTTnet.Client;
using System.Net.NetworkInformation;
using electrostore.Dto;

namespace electrostore.Services.ConfigService;

public class ConfigService : IConfigService
{
    private readonly IMqttClient _mqttClient;
    private readonly IConfiguration _configuration;

    public ConfigService(IMqttClient mqttClient, IConfiguration configuration)
    {
        _mqttClient = mqttClient;
        _configuration = configuration;
    }

    public async Task<ReadConfig> getAllConfig()
    {
        using var ping = new Ping();
        PingReply? reply;
        // ping juste one time in < 0.3s
        try {
            reply = await ping.SendPingAsync("electrostoreIA", 300);
        }
        catch (PingException) {
            reply = null;
        }
        return new ReadConfig {
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
            max_length_status = Constants.MaxStatusLength,
            max_size_document_in_mb = Constants.MaxDocumentSizeMB
        };
    }
}