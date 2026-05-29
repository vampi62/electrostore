

namespace ElectrostoreIA.Services.ConfigCacheService;

public interface IConfigCacheService
{
    public bool DemoMode { get; }
    public IEnumerable<string> AllowedImageExtensions { get; }
}