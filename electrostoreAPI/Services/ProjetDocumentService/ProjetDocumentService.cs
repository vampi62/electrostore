using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.ProjetDocumentService;

public class ProjetDocumentService : IProjetDocumentService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly FileService.FileService _fileService;
    private readonly string _projetDocumentsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments");

    public ProjetDocumentService(IMapper mapper, ApplicationDbContext context, FileService.FileService fileService)
    {
        _mapper = mapper;
        _context = context;
        _fileService = fileService;
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
        var savedFile = await _fileService.SaveFile(Path.Combine(_projetDocumentsPath, projetDocumentDto.id_projet.ToString()), projetDocumentDto.document);
        var projetDocument = new ProjetsDocuments
        {
            id_projet = projetDocumentDto.id_projet,
            url_projet_document = savedFile.url,
            name_projet_document = projetDocumentDto.name_projet_document,
            type_projet_document = savedFile.mimeType,
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
        await _fileService.DeleteFile(projetDocument.url_projet_document);
        _context.ProjetsDocuments.Remove(projetDocument);
        await _context.SaveChangesAsync();
    }
}