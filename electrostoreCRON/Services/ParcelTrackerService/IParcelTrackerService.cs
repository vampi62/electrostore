namespace ElectrostoreCRON.Services.ParcelTrackerService;

public interface IParcelTrackerService
{
    Task TrackAsync(string? paramsJson, CancellationToken ct = default);
}
