using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.CronJobService;

public interface ICronJobService
{
    public Task<PaginatedResponseDto<ReadCronJobDto>> GetCronJobs(int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<int>? idResearch = null);

    public Task<ReadCronJobDto> GetCronJobById(int id);

    public Task<ReadCronJobDto> CreateCronJob(CreateCronJobDto cronJobDto);

    public Task<ReadCronJobDto> UpdateCronJob(int id, UpdateCronJobDto cronJobDto);

    public Task DeleteCronJob(int id);
}
