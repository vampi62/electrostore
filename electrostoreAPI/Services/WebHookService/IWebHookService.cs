using ElectrostoreAPI.Dto;
using System.Text.Json;

namespace ElectrostoreAPI.Services.WebHookService;

public interface IWebHookService
{
    public Task Process17TrackWebhook(JsonElement body, string signatureHeader);
}