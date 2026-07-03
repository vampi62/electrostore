namespace ElectrostoreCRON.Services.Track17SyncService;

public interface ITrack17SyncService
{
    Task SyncAllAsync(CancellationToken ct = default);
}
