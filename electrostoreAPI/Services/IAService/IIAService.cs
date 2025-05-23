using electrostore.Dto;

namespace electrostore.Services.IAService;

public interface IIAService
{
    Task<IEnumerable<ReadIADto>> GetIA(int limit = 100, int offset = 0, List<int>? idResearch = null);

    Task<int> GetIACount();

    Task<ReadIADto> GetIAById(int id);

    Task<ReadIADto> CreateIA(CreateIADto iaDto);

    Task<ReadIADto> UpdateIA(int id, UpdateIADto iaDto);

    Task DeleteIA(int id);
    
    Task<IAStatusDto> GetIATrainingStatusById(int id);

    Task StartIATrainById(int id);

    Task<PredictionOutput> IADetectItem(int id, DetecDto detecDto);
}