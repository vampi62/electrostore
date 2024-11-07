using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.BoxTagService;

public interface IBoxTagService
{
    public Task<IEnumerable<ReadBoxTagDto>> GetBoxsTagsByBoxId(int boxId, int limit = 100, int offset = 0);

    public Task<IEnumerable<ReadBoxTagDto>> GetBoxsTagsByTagId(int tagId, int limit = 100, int offset = 0);

    public Task<ReadBoxTagDto> GetBoxTagById(int boxId, int tagId);

    public Task<IEnumerable<ReadBoxTagDto>> CreateBoxTags(int? boxId = null, int? tagId = null, int[]? tags = null, int[]? boxs = null);

    public Task<ReadBoxTagDto> CreateBoxTag(CreateBoxTagDto boxTag);

    public Task DeleteBoxTag(int boxId, int tagId);

    public Task CheckIfStoreExists(int storeId);
}