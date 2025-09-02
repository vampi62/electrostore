using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.ProjetDocumentService;

public class ProjetDocumentService : IProjetDocumentService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly string _projetDocumentsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments");

    public ProjetDocumentService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<IEnumerable<ReadProjetDocumentDto>> GetProjetDocumentsByProjetId(int projetId, int limit = 100, int offset = 0)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetId))
        {
            throw new KeyNotFoundException($"Projet with id {projetId} not found");
        }
        var query = _context.ProjetsDocuments.AsQueryable();
        query = query.Where(pd => pd.id_projet == projetId);
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(pd => pd.id_projet_document);
        var projetDocument = await query.ToListAsync();
        return _mapper.Map<List<ReadProjetDocumentDto>>(projetDocument);
    }

    public async Task<int> GetProjetDocumentsCountByProjetId(int projetId)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetId))
        {
            throw new KeyNotFoundException($"Projet with id {projetId} not found");
        }
        return await _context.ProjetsDocuments
            .CountAsync(pd => pd.id_projet == projetId);
    }

    public async Task<ReadProjetDocumentDto> GetProjetDocumentById(int id, int? projetId = null)
    {
        var projetDocument = await _context.ProjetsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"ProjetDocument with id {id} not found");
        if (projetId is not null && projetDocument.id_projet != projetId)
        {
            throw new KeyNotFoundException($"ProjetDocument with id {id} not found for projet with id {projetId}");
        }
        return _mapper.Map<ReadProjetDocumentDto>(projetDocument);
    }

    public async Task<ReadProjetDocumentDto> CreateProjetDocument(CreateProjetDocumentDto projetDocumentDto)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetDocumentDto.id_projet))
        {
            throw new KeyNotFoundException($"Projet with id {projetDocumentDto.id_projet} not found");
        }
        var fileName = Path.GetFileNameWithoutExtension(projetDocumentDto.document.FileName);
        fileName = fileName.Replace(".", "").Replace("/", ""); // remove "." and "/" from the file name to prevent directory traversal attacks
        if (fileName.Length > 100) // cut the file name to 100 characters to prevent too long file names
        {
            fileName = fileName[..100];
        }
        var fileExt = Path.GetExtension(projetDocumentDto.document.FileName);
        var i = 1;
        // verifie si un document avec le meme nom existe deja sur le serveur dans "wwwroot/projetDocuments"
        // si oui, on ajoute un numero a la fin du nom du document et on recommence la verification jusqu'a trouver un nom disponible
        var newName = fileName;
        while (File.Exists(Path.Combine(_projetDocumentsPath, projetDocumentDto.id_projet.ToString(), newName)))
        {
            newName = $"{fileName}({i}){fileExt}";
            i++;
        }
        var savePath = Path.Combine(_projetDocumentsPath, projetDocumentDto.id_projet.ToString(), newName);
        using (var fileStream = new FileStream(savePath, FileMode.Create))
        {
            await projetDocumentDto.document.CopyToAsync(fileStream);
        }
        var projetDocument = new ProjetsDocuments
        {
            id_projet = projetDocumentDto.id_projet,
            url_projet_document = projetDocumentDto.id_projet.ToString() + "/" + newName,
            name_projet_document = projetDocumentDto.name_projet_document,
            type_projet_document = fileExt.Replace(".", "").ToLowerInvariant(),
            size_projet_document = projetDocumentDto.document.Length
        };
        await _context.ProjetsDocuments.AddAsync(projetDocument);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjetDocumentDto>(projetDocument);
    }

    public async Task<ReadProjetDocumentDto> UpdateProjetDocument(int id, UpdateProjetDocumentDto projetDocumentDto, int? projetId = null)
    {
        var projetDocument = await _context.ProjetsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"ProjetDocument with id {id} not found");
        if (projetId is not null && projetDocument.id_projet != projetId)
        {
            throw new KeyNotFoundException($"ProjetDocument with id {id} not found for projet with id {projetId}");
        }
        if (projetDocumentDto.name_projet_document is not null)
        {
            projetDocument.name_projet_document = projetDocumentDto.name_projet_document;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjetDocumentDto>(projetDocument);
    }

    public async Task DeleteProjetDocument(int id, int? projetId = null)
    {
        var projetDocument = await _context.ProjetsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"ProjetDocument with id {id} not found");
        if (projetId is not null && projetDocument.id_projet != projetId)
        {
            throw new KeyNotFoundException($"ProjetDocument with id {id} not found for projet with id {projetId}");
        }
        var path = Path.Combine(_projetDocumentsPath, projetDocument.url_projet_document);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        _context.ProjetsDocuments.Remove(projetDocument);
        await _context.SaveChangesAsync();
    }
}