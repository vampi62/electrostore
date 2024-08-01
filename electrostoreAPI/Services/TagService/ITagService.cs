using electrostore.Dto;

namespace electrostore.Services.TagService;

public interface ITagService
{
    public Task<IEnumerable<ReadTagDto>> GetTags(int limit = 100, int offset = 0);

    public Task<ReadTagDto> GetTagById(int id);

    public Task<ReadTagDto> CreateTag(CreateTagDto tagDto);

    public Task<ReadTagDto> UpdateTag(int id, UpdateTagDto tagDto);

    public Task DeleteTag(int id);
}