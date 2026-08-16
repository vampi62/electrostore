using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.AIService;

public interface IAIService
{
    Task<PaginatedResponseDto<ReadAIDto>> GetIA(int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<int>? idResearch = null);

    Task<ReadAIDto> GetIAById(int id);

    Task<ReadAIDto> CreateIA(CreateAIDto aiDto);

    Task<ReadAIDto> UpdateIA(int id, UpdateAIDto aiDto);

    Task DeleteIA(int id);
    
    Task<AIStatusDto> GetIATrainingStatusById(int id);

    Task StartIATrainById(int id);

    Task<PredictionOutput> IADetectItem(int id, DetecDto detecDto);

    Task<bool> UpdateIaStatusAsync(int id, AIStatusDto status, int? requestedBy, CancellationToken cancellationToken);
}