using MQTTnet.Client;
using electrostore.Dto;
using System.Text.Json;

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
        try
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("http://electrostoreIA:5000/health");
            var content = await response.Content.ReadAsStringAsync();
            var status = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content) ?? throw new InvalidOperationException("Error while getting IA health status");
            return new ReadConfig
            {
                // get if the smtp is enabled
                smtp_enabled = _configuration["SMTP:Enable"] == "true",
                // check if the mqtt is connected
                mqtt_connected = _mqttClient.IsConnected,
                // check if ia status is healthy
                ia_connected = status.TryGetValue("status", out var statusValue) && statusValue.ValueKind == JsonValueKind.String && statusValue.GetString() == "healthy",
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
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error while getting configuration", ex);
        }
    }
}