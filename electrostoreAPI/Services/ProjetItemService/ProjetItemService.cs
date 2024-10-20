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

    public async Task<IEnumerable<ReadProjetItemDto>> GetProjetItemsByProjetId(int ProjetId, int limit = 100, int offset = 0)
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
            .Select(p => new ReadProjetItemDto
            {
                id_item = p.id_item,
                id_projet = p.id_projet,
                qte_projetitem = p.qte_projetitem,
                item = new ReadItemDto
                {
                    id_item = p.Item.id_item,
                    nom_item = p.Item.nom_item,
                    seuil_min_item = p.Item.seuil_min_item,
                    datasheet_item = p.Item.datasheet_item,
                    description_item = p.Item.description_item,
                    id_img = p.Item.id_img
                }
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<ReadProjetItemDto>> GetProjetItemsByItemId(int ItemId, int limit = 100, int offset = 0)
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
            .Select(p => new ReadProjetItemDto
            {
                id_item = p.id_item,
                id_projet = p.id_projet,
                qte_projetitem = p.qte_projetitem,
                item = new ReadItemDto
                {
                    id_item = p.Item.id_item,
                    nom_item = p.Item.nom_item,
                    seuil_min_item = p.Item.seuil_min_item,
                    datasheet_item = p.Item.datasheet_item,
                    description_item = p.Item.description_item,
                    id_img = p.Item.id_img
                }
            })
            .ToListAsync();
    }

    public async Task<ReadProjetItemDto> GetProjetItemById(int projetId, int itemId)
    {
        var projetItem = await _context.ProjetsItems.FindAsync(projetId, itemId) ?? throw new KeyNotFoundException($"ProjetItem with id {projetId} not found");
        return new ReadProjetItemDto
        {
            id_item = projetItem.id_item,
            id_projet = projetItem.id_projet,
            qte_projetitem = projetItem.qte_projetitem,
            item = new ReadItemDto
            {
                id_item = projetItem.Item.id_item,
                nom_item = projetItem.Item.nom_item,
                seuil_min_item = projetItem.Item.seuil_min_item,
                datasheet_item = projetItem.Item.datasheet_item,
                description_item = projetItem.Item.description_item,
                id_img = projetItem.Item.id_img
            }
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
            qte_projetitem = projetItemDto.qte_projetitem
        };
        _context.ProjetsItems.Add(newProjetItem);
        await _context.SaveChangesAsync();
        return new ReadProjetItemDto
        {
            id_item = newProjetItem.id_item,
            id_projet = newProjetItem.id_projet,
            qte_projetitem = newProjetItem.qte_projetitem,
            item = new ReadItemDto
            {
                id_item = newProjetItem.Item.id_item,
                nom_item = newProjetItem.Item.nom_item,
                seuil_min_item = newProjetItem.Item.seuil_min_item,
                datasheet_item = newProjetItem.Item.datasheet_item,
                description_item = newProjetItem.Item.description_item,
                id_img = newProjetItem.Item.id_img
            }
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
        if (projetItemDto.qte_projetitem != null)
        {
            projetItemToUpdate.qte_projetitem = projetItemDto.qte_projetitem.Value;
        }
        await _context.SaveChangesAsync();
        return new ReadProjetItemDto
        {
            id_item = projetItemToUpdate.id_item,
            id_projet = projetItemToUpdate.id_projet,
            qte_projetitem = projetItemToUpdate.qte_projetitem,
            item = new ReadItemDto
            {
                id_item = projetItemToUpdate.Item.id_item,
                nom_item = projetItemToUpdate.Item.nom_item,
                seuil_min_item = projetItemToUpdate.Item.seuil_min_item,
                datasheet_item = projetItemToUpdate.Item.datasheet_item,
                description_item = projetItemToUpdate.Item.description_item,
                id_img = projetItemToUpdate.Item.id_img
            }
        };
    }

    public async Task DeleteProjetItem(int projetId, int itemId)
    {
        var projetItemToDelete = await _context.ProjetsItems.FindAsync(projetId, itemId) ?? throw new KeyNotFoundException($"ProjetItem with id_projet {projetId} and id_item {itemId} not found");
        _context.ProjetsItems.Remove(projetItemToDelete);
        await _context.SaveChangesAsync();
    }
}