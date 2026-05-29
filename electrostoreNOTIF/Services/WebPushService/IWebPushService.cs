namespace ElectrostoreNOTIF.Services.WebPushService;

public interface IWebPushService
{
    Task SendAsync(string endpoint, string p256dh, string auth, string title, string body, Dictionary<string, string>? data = null);
}
