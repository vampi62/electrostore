using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.WebHookService;

public interface IWebHookService
{
    public Task<bool> Process17TrackWebhook(string body);
}