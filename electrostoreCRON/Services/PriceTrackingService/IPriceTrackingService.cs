namespace ElectrostoreCRON.Services.PriceTrackingService;

public interface IPriceTrackingService
{
    Task SyncAllAsync(CancellationToken ct = default);
}
