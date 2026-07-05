using System.Text.Json;
using WebPush;

namespace ElectrostoreNOTIF.Services.WebPushService;

public class WebPushService : IWebPushService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebPushService> _logger;
    private readonly WebPushClient _client;
    private readonly VapidDetails _vapid;

    public WebPushService(IConfiguration configuration, ILogger<WebPushService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        var publicKey = configuration["VAPID:PublicKey"] ?? throw new InvalidOperationException("VAPID:PublicKey is not configured.");
        var privateKey = configuration["VAPID:PrivateKey"] ?? throw new InvalidOperationException("VAPID:PrivateKey is not configured.");
        var subject = configuration["VAPID:Subject"] ?? "mailto:admin@electrostore.local";
        _vapid = new VapidDetails(subject, publicKey, privateKey);
        _client = new WebPushClient();
    }

    public async Task SendAsync(string endpoint, string p256dh, string auth, string title, string body, Dictionary<string, string>? data = null)
    {

        if (!bool.TryParse(_configuration["VAPID:Enable"], out var isEnabled) || !isEnabled)
        {
            _logger.LogDebug("VAPID disabled - push notification ignored for {Endpoint}", endpoint);
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            title,
            body,
            data
        });

        var subscription = new PushSubscription(endpoint, p256dh, auth);
        try
        {
            await _client.SendNotificationAsync(subscription, payload, _vapid);
            _logger.LogInformation("[WebPushService] Push notification sent to {Endpoint}", endpoint);
        }
        catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone ||
                                           ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                "[WebPushService] Subscription expired or invalid (endpoint={Endpoint}): {Status}",
                endpoint, ex.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WebPushService] Failed to send push to {Endpoint}", endpoint);
            throw;
        }
    }
}
