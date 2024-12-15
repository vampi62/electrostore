using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ProjetItemService;

public class ProjetItemService : IProjetItemService
{
    private readonly ApplicationDbContext _context;

    public ProjetItemService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadExtendedProjetItemDto>> GetProjetItemsByProjetId(int ProjetId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == ProjetId))
        {
            throw new KeyNotFoundException($"Projet with id {ProjetId} not found");
        }
        return await _context.ProjetsItems
            .Skip(offset)
            .Take(limit)
            .Where(p => p.id_projet == ProjetId)
            .Select(p => new ReadExtendedProjetItemDto
            {
                id_item = p.id_item,
                id_projet = p.id_projet,
                qte_projet_item = p.qte_projet_item,
                item = expand != null && expand.Contains("item") ? new ReadItemDto
                {
                    id_item = p.Item.id_item,
                    nom_item = p.Item.nom_item,
                    seuil_min_item = p.Item.seuil_min_item,
                    description_item = p.Item.description_item,
                    id_img = p.Item.id_img
                } : null,
                projet = expand != null && expand.Contains("projet") ? new ReadProjetDto
                {
                    id_projet = p.Projet.id_projet,
                    nom_projet = p.Projet.nom_projet,
                    description_projet = p.Projet.description_projet,
                    url_projet = p.Projet.url_projet,
                    status_projet = p.Projet.status_projet,
                    date_debut_projet = p.Projet.date_debut_projet,
                    date_fin_projet = p.Projet.date_fin_projet
                } : null
            })
            .ToListAsync();
    }

    public async Task<int> GetProjetItemsCountByProjetId(int ProjetId)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == ProjetId))
        {
            throw new KeyNotFoundException($"Projet with id {ProjetId} not found");
        }
        return await _context.ProjetsItems
            .CountAsync(p => p.id_projet == ProjetId);
    }

    public async Task<IEnumerable<ReadExtendedProjetItemDto>> GetProjetItemsByItemId(int ItemId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == ItemId))
        {
            throw new KeyNotFoundException($"Item with id {ItemId} not found");
        }
        return await _context.ProjetsItems
            .Skip(offset)
            .Take(limit)
            .Where(p => p.id_item == ItemId)
            .Select(p => new ReadExtendedProjetItemDto
            {
                id_item = p.id_item,
                id_projet = p.id_projet,
                qte_projet_item = p.qte_projet_item,
                item = expand != null && expand.Contains("item") ? new ReadItemDto
                {
                    id_item = p.Item.id_item,
                    nom_item = p.Item.nom_item,
                    seuil_min_item = p.Item.seuil_min_item,
                    description_item = p.Item.description_item,
                    id_img = p.Item.id_img
                } : null,
                projet = expand != null && expand.Contains("projet") ? new ReadProjetDto
                {
                    id_projet = p.Projet.id_projet,
                    nom_projet = p.Projet.nom_projet,
                    description_projet = p.Projet.description_projet,
                    url_projet = p.Projet.url_projet,
                    status_projet = p.Projet.status_projet,
                    date_debut_projet = p.Projet.date_debut_projet,
                    date_fin_projet = p.Projet.date_fin_projet
                } : null
            })
            .ToListAsync();
    }

    public async Task<int> GetProjetItemsCountByItemId(int ItemId)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == ItemId))
        {
            throw new KeyNotFoundException($"Item with id {ItemId} not found");
        }
        return await _context.ProjetsItems
            .CountAsync(p => p.id_item == ItemId);
    }

    public async Task<ReadExtendedProjetItemDto> GetProjetItemById(int projetId, int itemId, List<string>? expand = null)
    {
        var projetItem = await _context.ProjetsItems.FindAsync(projetId, itemId) ?? throw new KeyNotFoundException($"ProjetItem with id {projetId} not found");
        return new ReadExtendedProjetItemDto
        {
            id_item = projetItem.id_item,
            id_projet = projetItem.id_projet,
            qte_projet_item = projetItem.qte_projet_item,
            item = expand != null && expand.Contains("item") ? new ReadItemDto
            {
                id_item = projetItem.Item.id_item,
                nom_item = projetItem.Item.nom_item,
                seuil_min_item = projetItem.Item.seuil_min_item,
                description_item = projetItem.Item.description_item,
                id_img = projetItem.Item.id_img
            } : null,
            projet = expand != null && expand.Contains("projet") ? new ReadProjetDto
            {
                id_projet = projetItem.Projet.id_projet,
                nom_projet = projetItem.Projet.nom_projet,
                description_projet = projetItem.Projet.description_projet,
                url_projet = projetItem.Projet.url_projet,
                status_projet = projetItem.Projet.status_projet,
                date_debut_projet = projetItem.Projet.date_debut_projet,
                date_fin_projet = projetItem.Projet.date_fin_projet
            } : null
        };
    }

    public async Task<ReadProjetItemDto> CreateProjetItem(CreateProjetItemDto projetItemDto)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetItemDto.id_projet))
        {
            throw new KeyNotFoundException($"Projet with id {projetItemDto.id_projet} not found");
        }
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == projetItemDto.id_item))
        {
            throw new KeyNotFoundException($"Item with id {projetItemDto.id_item} not found");
        }
        // check if the projetItem already exists
        if (await _context.ProjetsItems.AnyAsync(p => p.id_projet == projetItemDto.id_projet && p.id_item == projetItemDto.id_item))
        {
            throw new InvalidOperationException($"ProjetItem with id_projet {projetItemDto.id_projet} and id_item {projetItemDto.id_item} already exists");
        }
        var newProjetItem = new ProjetsItems
        {
            id_projet = projetItemDto.id_projet,
            id_item = projetItemDto.id_item,
            qte_projet_item = projetItemDto.qte_projet_item
        };
        _context.ProjetsItems.Add(newProjetItem);
        await _context.SaveChangesAsync();
        return new ReadProjetItemDto
        {
            id_item = newProjetItem.id_item,
            id_projet = newProjetItem.id_projet,
            qte_projet_item = newProjetItem.qte_projet_item
        };
    }

    public async Task<ReadBulkProjetItemDto> CreateBulkProjetItem(List<CreateProjetItemDto> projetItemBulkDto)
    {
        var validQuery = new List<ReadProjetItemDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var projetItemDto in projetItemBulkDto)
        {
            try
            {
                validQuery.Add(await CreateProjetItem(projetItemDto));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = projetItemDto
                });
            }
        }
        return new ReadBulkProjetItemDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task<ReadProjetItemDto> UpdateProjetItem(int projetId, int itemId, UpdateProjetItemDto projetItemDto)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetId))
        {
            throw new KeyNotFoundException($"Projet with id {projetId} not found");
        }
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id {itemId} not found");
        }
        var projetItemToUpdate = await _context.ProjetsItems.FindAsync(projetId, itemId) ?? throw new KeyNotFoundException($"ProjetItem with id_projet {projetId} and id_item {itemId} not found");
        if (projetItemDto.qte_projet_item is not null)
        {
            projetItemToUpdate.qte_projet_item = projetItemDto.qte_projet_item.Value;
        }
        await _context.SaveChangesAsync();
        return new ReadProjetItemDto
        {
            id_item = projetItemToUpdate.id_item,
            id_projet = projetItemToUpdate.id_projet,
            qte_projet_item = projetItemToUpdate.qte_projet_item
        };
    }

    public async Task DeleteProjetItem(int projetId, int itemId)
    {
        var projetItemToDelete = await _context.ProjetsItems.FindAsync(projetId, itemId) ?? throw new KeyNotFoundException($"ProjetItem with id_projet {projetId} and id_item {itemId} not found");
        _context.ProjetsItems.Remove(projetItemToDelete);
        await _context.SaveChangesAsync();
    }
}