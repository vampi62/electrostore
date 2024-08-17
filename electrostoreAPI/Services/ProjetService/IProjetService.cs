using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ProjetService;

public interface IProjetService
{
    public Task<IEnumerable<ReadProjetDto>> GetProjets(int limit = 100, int offset = 0);

    public Task<ActionResult<ReadProjetDto>> GetProjetById(int id);

    public Task<ReadProjetDto> CreateProjet(CreateProjetDto projetDto);

    public Task<ActionResult<ReadProjetDto>> UpdateProjet(int id, UpdateProjetDto projetDto);

    public Task<IActionResult> DeleteProjet(int id);
}