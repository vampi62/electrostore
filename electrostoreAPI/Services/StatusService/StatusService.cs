using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Kafka.Producer;
using MQTTnet;
using System.Text.Json;

namespace ElectrostoreAPI.Services.StatusService;

public class StatusService : IStatusService
{
    private readonly IMqttClient _mqttClient;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApplicationDbContext _context;
    private readonly IKafkaProducerService _kafkaProducerService;

    public StatusService(
        IMqttClient mqttClient,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ApplicationDbContext context,
        IKafkaProducerService kafkaProducerService)
    {
        _mqttClient = mqttClient;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _context = context;
        _kafkaProducerService = kafkaProducerService;
    }

    public async Task<ReadStatusDto> GetStatus()
    {
        var notifUrl = _configuration.GetValue<string>("NotifServiceHealthUrl") ?? throw new InvalidOperationException("NotifServiceHealthUrl configuration is missing.");
        var cronUrl = _configuration.GetValue<string>("CRONServiceHealthUrl") ?? throw new InvalidOperationException("CRONServiceHealthUrl configuration is missing.");
        var workerUrl = _configuration.GetValue<string>("WORKERServiceHealthUrl") ?? throw new InvalidOperationException("WORKERServiceHealthUrl configuration is missing.");

        var notifTask = FetchServiceHealth(notifUrl);
        var cronTask = FetchServiceHealth(cronUrl);
        var workerTask = FetchServiceHealth(workerUrl);
        var dbTask = CheckDatabaseAsync();
        var kafkaTask = _kafkaProducerService.IsConnectedAsync();

        await Task.WhenAll(notifTask, cronTask, workerTask, dbTask, kafkaTask);

        var notifHealth = notifTask.Result;
        var cronHealth = cronTask.Result;
        var workerHealth = workerTask.Result;

        return new ReadStatusDto
        {
            api_status = _configuration.GetValue<bool>("DemoMode") ? "demo" : "healthy",
            db_connected = dbTask.Result,
            mqtt_connected = _mqttClient.IsConnected,
            kafka_connected = kafkaTask.Result,
            llm_status = "unknown",
            stt_status = "unknown",
            notif_status = notifHealth.TryGetValue("status", out var notifStatus) && notifStatus.GetString() is string ns ? ns : "unknown",
            notif_smtp = notifHealth.TryGetValue("smtp", out var smtpElement) && (smtpElement.ValueKind == JsonValueKind.True || smtpElement.ValueKind == JsonValueKind.False) ? smtpElement.ValueKind == JsonValueKind.True : (bool?)false,
            notif_web_push = notifHealth.TryGetValue("webPush", out var wpElement) && (wpElement.ValueKind == JsonValueKind.True || wpElement.ValueKind == JsonValueKind.False) ? wpElement.ValueKind == JsonValueKind.True : (bool?)false,
            cron_status = cronHealth.TryGetValue("status", out var cronStatus) && cronStatus.GetString() is string cs ? cs : "unknown",
            worker_status = workerHealth.TryGetValue("status", out var workerStatus) && workerStatus.GetString() is string workerStr ? workerStr : "unknown",
            external_services = new Dictionary<string, string>
            {}
        };
    }

    private async Task<bool> CheckDatabaseAsync()
    {
        try
        {
            return await _context.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }

    private async Task<Dictionary<string, JsonElement>> FetchServiceHealth(string url)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            var response = await httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content)
                ?? new Dictionary<string, JsonElement> { { "status", JsonDocument.Parse("\"unknown\"").RootElement } };
        }
        catch
        {
            return new Dictionary<string, JsonElement>
            {
                { "status", JsonDocument.Parse("\"unreachable\"").RootElement }
            };
        }
    }
}
