using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.ProjectItemService;

public interface IProjectItemService
{
    public Task<PaginatedResponseDto<ReadExtendedProjectItemDto>> GetProjetItemsByProjetId(int projetId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<PaginatedResponseDto<ReadExtendedProjectItemDto>> GetProjetItemsByItemId(int itemId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<ReadExtendedProjectItemDto> GetProjetItemById(int projetId, int itemId, List<string>? expand = null);

    public Task<ReadProjectItemDto> CreateProjetItem(CreateProjectItemDto projetItemDto);

    public Task<ReadBulkProjectItemDto> CreateBulkProjetItem(List<CreateProjectItemDto> projetItemBulkDto);

    public Task<ReadProjectItemDto> UpdateProjetItem(int projetId, int itemId, UpdateProjectItemDto projetItemDto);

    public Task DeleteProjetItem(int projetId, int itemId);
}