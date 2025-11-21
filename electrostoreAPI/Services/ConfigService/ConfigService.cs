using MQTTnet;
using electrostore.Dto;
using System.Text.Json;

namespace electrostore.Services.ConfigService;

public class ConfigService : IConfigService
{
    private readonly IMqttClient _mqttClient;
    private readonly IConfiguration _configuration;
    private readonly string _iaServiceUrl = "http://electrostoreIA:5000/health";

    public ConfigService(IMqttClient mqttClient, IConfiguration configuration)
    {
        _mqttClient = mqttClient;
        _configuration = configuration;
    }

    public async Task<ReadConfig> getAllConfig()
    {
        Dictionary<string, JsonElement> status;
        try
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(_iaServiceUrl);
            var content = await response.Content.ReadAsStringAsync();
            status = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content) ?? throw new InvalidOperationException("Error while getting IA health status");
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            Console.WriteLine($"Error fetching IA health status: {ex.Message}");
            // Optionally, you can set a default value for status if needed
            status = new Dictionary<string, JsonElement>
            {
                { "status", JsonDocument.Parse("\"unknown\"").RootElement }
            };
        }
        return new ReadConfig
        {
            // get if the smtp is enabled
            smtp_enabled = _configuration["SMTP:Enable"] == "true",
            // check if the mqtt is connected
            mqtt_connected = _mqttClient.IsConnected,
            // return the IA service status
            ia_service_status = status.TryGetValue("status", out var statusElement) && statusElement.GetString() != null ? statusElement.GetString()! : string.Empty,
            // check if the demo mode is enabled
            demo_mode = _configuration.GetValue<bool>("DemoMode"),
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
            max_size_document_in_mb = Constants.MaxDocumentSizeMB,
            sso_available_providers = _configuration.GetSection("OAuth").GetChildren().Select(provider => new SSOAvailableProvider
            {
                provider = provider.Key,
                display_name = provider.GetValue<string>("DisplayName") ?? string.Empty,
                icon_url = provider.GetValue<string>("IconUrl") ?? string.Empty
            }).ToList()
        };
    }
}