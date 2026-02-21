using electrostore.Dto;

namespace electrostore.Services.BoxTagService;

public interface IBoxTagService
{
    public Task<PaginatedResponseDto<ReadExtendedBoxTagDto>> GetBoxsTagsByBoxId(int boxId, int limit = 100, int offset = 0, List<string>? expand = null);

    public Task<PaginatedResponseDto<ReadExtendedBoxTagDto>> GetBoxsTagsByTagId(int tagId, int limit = 100, int offset = 0, List<string>? expand = null);

    public Task<ReadExtendedBoxTagDto> GetBoxTagById(int boxId, int tagId, List<string>? expand = null);

    public Task<ReadBoxTagDto> CreateBoxTag(CreateBoxTagDto boxTagDto);

    public Task<ReadBulkBoxTagDto> CreateBulkBoxTag(List<CreateBoxTagDto> boxTagBulkDto);

    public Task DeleteBoxTag(int boxId, int tagId);

    public Task CheckIfStoreExists(int storeId, int boxId);

    public Task<ReadBulkBoxTagDto> DeleteBulkBoxTag(List<CreateBoxTagDto> boxTagBulkDto);
}