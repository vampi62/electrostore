using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ProjetCommentaireService;

public interface IProjetCommentaireService
{
    public Task<IEnumerable<ReadProjetCommentaireDto>> GetProjetCommentairesByProjetId(int projetId, int limit = 100, int offset = 0);

    public Task<IEnumerable<ReadProjetCommentaireDto>> GetProjetCommentairesByUserId(int userId, int limit = 100, int offset = 0);

    public Task<ReadProjetCommentaireDto> GetProjetCommentairesByCommentaireId(int id, int? userId = null, int? projetId = null);

    public Task<ReadProjetCommentaireDto> CreateProjetCommentaire(CreateProjetCommentaireDto projetCommentaireDto);

    public Task<ReadProjetCommentaireDto> UpdateProjetCommentaire(int id, UpdateProjetCommentaireDto projetCommentaireDto, int? userId = null, int? projetId = null);

    public Task DeleteProjetCommentaire(int id, int? userId = null, int? projetId = null);
}