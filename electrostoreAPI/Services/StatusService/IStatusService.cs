using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.StatusService;

public interface IStatusService
{
    Task<ReadStatusDto> GetStatus();
}
