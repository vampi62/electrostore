using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.TagService;

public interface ITagService
{
    public Task<IEnumerable<ReadTagDto>> GetTags(int limit = 100, int offset = 0);

    public Task<ActionResult<ReadTagDto>> GetTagById(int id);

    public Task<ActionResult<ReadTagDto>> CreateTag(CreateTagDto tagDto);

    public Task<ActionResult<ReadTagDto>> UpdateTag(int id, UpdateTagDto tagDto);

    public Task<IActionResult> DeleteTag(int id);
}