using electrostore.Dto;

namespace electrostore.Services.ConfigService;

public interface IConfigService
{
    Task<ReadConfig> getAllConfig();
}