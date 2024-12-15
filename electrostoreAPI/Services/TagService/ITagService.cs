using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.TagService;

public interface ITagService
{
    public Task<IEnumerable<ReadExtendedTagDto>> GetTags(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null);

    public Task<int> GetTagsCount();

    public Task<ReadExtendedTagDto> GetTagById(int id, List<string>? expand = null);

    public Task<ReadTagDto> CreateTag(CreateTagDto tagDto);

    public Task<ReadBulkTagDto> CreateBulkTag(List<CreateTagDto> tagBulkDto);

    public Task<ReadTagDto> UpdateTag(int id, UpdateTagDto tagDto);

    public Task DeleteTag(int id);
}