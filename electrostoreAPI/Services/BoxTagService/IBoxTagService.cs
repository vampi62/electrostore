using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.BoxTagService;

public interface IBoxTagService
{
    public Task<ActionResult<IEnumerable<ReadBoxTagDto>>> GetBoxsTagsByBoxId(int boxId, int limit = 100, int offset = 0);

    public Task<ActionResult<IEnumerable<ReadBoxTagDto>>> GetBoxsTagsByTagId(int tagId, int limit = 100, int offset = 0);

    public Task<ActionResult<ReadBoxTagDto>> GetBoxTagById(int boxId, int tagId);

    public Task<ActionResult<ReadBoxTagDto>> CreateBoxTag(CreateBoxTagDto boxTag);

    public Task<IActionResult> DeleteBoxTag(int boxId, int tagId);
}