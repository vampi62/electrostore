namespace ElectrostoreAPI.Services.SttService;

public interface ISttService
{
    bool IsEnabled { get; }

    Task<string> TranscribeAsync(IFormFile audioFile, CancellationToken cancellationToken = default);
}
