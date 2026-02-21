using electrostore.Dto;

namespace electrostore.Services.ProjetCommentaireService;

public interface IProjetCommentaireService
{
    public Task<PaginatedResponseDto<ReadExtendedProjetCommentaireDto>> GetProjetCommentairesByProjetId(int projetId, int limit = 100, int offset = 0, List<string>? expand = null);

    public Task<PaginatedResponseDto<ReadExtendedProjetCommentaireDto>> GetProjetCommentairesByUserId(int userId, int limit = 100, int offset = 0, List<string>? expand = null);

    public Task<ReadExtendedProjetCommentaireDto> GetProjetCommentairesById(int id, int? userId = null, int? projetId = null, List<string>? expand = null);

    public Task<ReadProjetCommentaireDto> CreateProjetCommentaire(CreateProjetCommentaireDto projetCommentaireDto);

    public Task<ReadProjetCommentaireDto> UpdateProjetCommentaire(int id, UpdateProjetCommentaireDto projetCommentaireDto, int? userId = null, int? projetId = null);

    public Task DeleteProjetCommentaire(int id, int? userId = null, int? projetId = null);
}