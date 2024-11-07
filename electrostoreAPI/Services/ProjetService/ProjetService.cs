using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ProjetService;

public class ProjetService : IProjetService
{
    private readonly ApplicationDbContext _context;

    public ProjetService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadProjetDto>> GetProjets(int limit = 100, int offset = 0)
    {
        return await _context.Projets
            .Skip(offset)
            .Take(limit)
            .Select(p => new ReadProjetDto
            {
                id_projet = p.id_projet,
                nom_projet = p.nom_projet,
                description_projet = p.description_projet,
                url_projet = p.url_projet,
                status_projet = p.status_projet,
                date_projet = p.date_projet,
                date_fin_projet = p.date_fin_projet
            })
            .ToListAsync();
    }

    public async Task<ReadProjetDto> GetProjetById(int id)
    {
        var projet = await _context.Projets.FindAsync(id) ?? throw new KeyNotFoundException($"Projet with id {id} not found");
        return new ReadProjetDto
        {
            id_projet = projet.id_projet,
            nom_projet = projet.nom_projet,
            description_projet = projet.description_projet,
            url_projet = projet.url_projet,
            status_projet = projet.status_projet,
            date_projet = projet.date_projet,
            date_fin_projet = projet.date_fin_projet
        };
    }

    public async Task<ReadProjetDto> CreateProjet(CreateProjetDto projetDto)
    {
        var newProjet = new Projets
        {
            nom_projet = projetDto.nom_projet,
            description_projet = projetDto.description_projet,
            url_projet = projetDto.url_projet,
            status_projet = projetDto.status_projet,
            date_projet = projetDto.date_projet,
            date_fin_projet = projetDto.date_fin_projet
        };
        _context.Projets.Add(newProjet);
        await _context.SaveChangesAsync();
        if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments", newProjet.id_projet.ToString())))
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments", newProjet.id_projet.ToString()));
        }
        return new ReadProjetDto
        {
            id_projet = newProjet.id_projet,
            nom_projet = newProjet.nom_projet,
            description_projet = newProjet.description_projet,
            url_projet = newProjet.url_projet,
            status_projet = newProjet.status_projet,
            date_projet = newProjet.date_projet,
            date_fin_projet = newProjet.date_fin_projet
        };
    }

    public async Task<ReadProjetDto> UpdateProjet(int id, UpdateProjetDto projetDto)
    {
        var projetToUpdate = await _context.Projets.FindAsync(id) ?? throw new KeyNotFoundException($"Projet with id {id} not found");
        if (projetDto.nom_projet != null)
        {
            projetToUpdate.nom_projet = projetDto.nom_projet;
        }
        if (projetDto.description_projet != null)
        {
            projetToUpdate.description_projet = projetDto.description_projet;
        }
        if (projetDto.url_projet != null)
        {
            projetToUpdate.url_projet = projetDto.url_projet;
        }
        if (projetDto.status_projet != null)
        {
            projetToUpdate.status_projet = projetDto.status_projet;
        }
        if (projetDto.date_projet != null)
        {
            projetToUpdate.date_projet = projetDto.date_projet.Value;
        }
        if (projetDto.date_fin_projet != null)
        {
            projetToUpdate.date_fin_projet = projetDto.date_fin_projet;
        }
        await _context.SaveChangesAsync();
        return new ReadProjetDto
        {
            id_projet = projetToUpdate.id_projet,
            nom_projet = projetToUpdate.nom_projet,
            description_projet = projetToUpdate.description_projet,
            url_projet = projetToUpdate.url_projet,
            status_projet = projetToUpdate.status_projet,
            date_projet = projetToUpdate.date_projet,
            date_fin_projet = projetToUpdate.date_fin_projet
        };
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