using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.ProjetService;

public class ProjetService : IProjetService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public ProjetService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<IEnumerable<ReadExtendedProjetDto>> GetProjets(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Projets.AsQueryable();
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(b => idResearch.Contains(b.id_projet));
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("projets_commentaires"))
        {
            query = query.Include(p => p.ProjetsCommentaires);
        }
        if (expand != null && expand.Contains("projets_documents"))
        {
            query = query.Include(p => p.ProjetsDocuments);
        }
        if (expand != null && expand.Contains("projets_items"))
        {
            query = query.Include(p => p.ProjetsItems);
        }
        var projet = await query.ToListAsync();
        return _mapper.Map<List<ReadExtendedProjetDto>>(projet);
    }

    public async Task<int> GetProjetsCount()
    {
        return await _context.Projets.CountAsync();
    }

    public async Task<ReadExtendedProjetDto> GetProjetById(int id, List<string>? expand = null)
    {
        var query = _context.Projets.AsQueryable();
        query = query.Where(p => p.id_projet == id);
        if (expand != null && expand.Contains("projets_commentaires"))
        {
            query = query.Include(p => p.ProjetsCommentaires);
        }
        if (expand != null && expand.Contains("projets_documents"))
        {
            query = query.Include(p => p.ProjetsDocuments);
        }
        if (expand != null && expand.Contains("projets_items"))
        {
            query = query.Include(p => p.ProjetsItems);
        }
        var projet = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Projet with id {id} not found");
        return _mapper.Map<ReadExtendedProjetDto>(projet);
    }

    public async Task<ReadProjetDto> CreateProjet(CreateProjetDto projetDto)
    {
        var newProjet = new Projets
        {
            nom_projet = projetDto.nom_projet,
            description_projet = projetDto.description_projet,
            url_projet = projetDto.url_projet,
            status_projet = projetDto.status_projet,
            date_debut_projet = projetDto.date_debut_projet,
            date_fin_projet = projetDto.date_fin_projet
        };
        _context.Projets.Add(newProjet);
        await _context.SaveChangesAsync();
        if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments", newProjet.id_projet.ToString())))
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments", newProjet.id_projet.ToString()));
        }
        return _mapper.Map<ReadProjetDto>(newProjet);
    }

    public async Task<ReadProjetDto> UpdateProjet(int id, UpdateProjetDto projetDto)
    {
        var projetToUpdate = await _context.Projets.FindAsync(id) ?? throw new KeyNotFoundException($"Projet with id {id} not found");
        if (projetDto.nom_projet is not null)
        {
            projetToUpdate.nom_projet = projetDto.nom_projet;
        }
        if (projetDto.description_projet is not null)
        {
            projetToUpdate.description_projet = projetDto.description_projet;
        }
        if (projetDto.url_projet is not null)
        {
            projetToUpdate.url_projet = projetDto.url_projet;
        }
        if (projetDto.status_projet is not null)
        {
            projetToUpdate.status_projet = projetDto.status_projet;
        }
        if (projetDto.date_debut_projet is not null)
        {
            projetToUpdate.date_debut_projet = projetDto.date_debut_projet.Value;
        }
        if (projetDto.date_fin_projet is not null)
        {
            projetToUpdate.date_fin_projet = projetDto.date_fin_projet;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjetDto>(projetToUpdate);
    }

    public async Task DeleteProjet(int id)
    {
        var projetToDelete = await _context.Projets.FindAsync(id) ?? throw new KeyNotFoundException($"Projet with id {id} not found");
        _context.Projets.Remove(projetToDelete);
        await _context.SaveChangesAsync();
        //remove folder in wwwroot/projetDocuments
        if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments", id.ToString())))
        {
            Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments", id.ToString()), true);
        }
    }
}