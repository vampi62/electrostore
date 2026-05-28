using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.ConfigService;

public interface IConfigService
{
    Task<ReadConfig> getAllConfig();
    bool GetDemoMode();
    string[] GetAllowedImageExtensions();
}