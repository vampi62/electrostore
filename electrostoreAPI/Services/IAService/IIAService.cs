using electrostore.Dto;

namespace electrostore.Services.IAService;

public interface IIAService
{
    Task<List<ReadIADto>> GetIA(int limit = 100, int offset = 0);

    Task<ReadIADto> GetIAById(int id);

    Task<ReadIADto> CreateIA(CreateIADto IADto);

    Task<ReadIADto> UpdateIA(int id, UpdateIADto IADto);

    Task DeleteIA(int id);
}