using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.ProjectCommentService;

public interface IProjectCommentService
{
    public Task<PaginatedResponseDto<ReadExtendedProjectCommentDto>> GetProjetCommentairesByProjetId(int projetId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<PaginatedResponseDto<ReadExtendedProjectCommentDto>> GetProjetCommentairesByUserId(int userId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<ReadExtendedProjectCommentDto> GetProjetCommentairesById(int id, int? userId = null, int? projetId = null, List<string>? expand = null);

    public Task<ReadProjectCommentDto> CreateProjetCommentaire(CreateProjectCommentDto projetCommentaireDto);

    public Task<ReadProjectCommentDto> UpdateProjetCommentaire(int id, UpdateProjectCommentDto projetCommentaireDto, int? userId = null, int? projetId = null);

    public Task DeleteProjetCommentaire(int id, int? userId = null, int? projetId = null);
}