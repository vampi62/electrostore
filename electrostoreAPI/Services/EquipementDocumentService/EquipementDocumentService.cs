using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.FileService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.EquipementDocumentService;

public class EquipementDocumentService : IEquipementDocumentService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly string _equipementDocumentsPath = "equipementDocuments";

    public EquipementDocumentService(IMapper mapper, ApplicationDbContext context, IFileService fileService)
    {
        _mapper = mapper;
        _context = context;
        _fileService = fileService;
    }

    public async Task<PaginatedResponseDto<ReadEquipementDocumentDto>> GetEquipementsDocumentsByEquipementId(int equipementId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null)
    {
        // check if equipement exists
        if (!await _context.Equipements.AnyAsync(e => e.id_equipement == equipementId))
        {
            throw new KeyNotFoundException($"Equipement with id '{equipementId}' not found");
        }
        var query = _context.EquipementsDocuments.AsQueryable();
        var filterResult = default(Expression<Func<EquipementsDocuments, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { field = "id_equipement", search_type = "eq", value = equipementId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<EquipementsDocuments>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<EquipementsDocuments>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { field = "id_equipement_document", order = "asc" };
                query = query.OrderBy(ed => ed.id_equipement_document);
            }
        }
        else
        {
            query = query.OrderBy(ed => ed.id_equipement_document);
        }
        query = query.Skip(offset).Take(limit);
        var equipementDocument = await query.ToListAsync();
        return new PaginatedResponseDto<ReadEquipementDocumentDto>
        {
            data = _mapper.Map<List<ReadEquipementDocumentDto>>(equipementDocument),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.EquipementsDocuments.CountAsync(filterResult ?? (ed => ed.id_equipement == equipementId)),
                next_offset = offset + limit,
                has_more = await _context.EquipementsDocuments.Skip(offset + limit).AnyAsync(filterResult ?? (ed => ed.id_equipement == equipementId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadEquipementDocumentDto> GetEquipementDocumentById(int id, int? equipementId = null)
    {
        var equipementDocument = await _context.EquipementsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"EquipementDocument with id '{id}' not found");
        if (equipementId is not null && equipementDocument.id_equipement != equipementId)
        {
            throw new KeyNotFoundException($"EquipementDocument with id '{id}' not found for equipement with id '{equipementId}'");
        }
        return _mapper.Map<ReadEquipementDocumentDto>(equipementDocument);
    }

    public async Task<ReadEquipementDocumentDto> CreateEquipementDocument(CreateEquipementDocumentDto equipementDocumentDto)
    {
        // check if equipement exists
        if (!await _context.Equipements.AnyAsync(e => e.id_equipement == equipementDocumentDto.id_equipement))
        {
            throw new KeyNotFoundException($"Equipement with id '{equipementDocumentDto.id_equipement}' not found");
        }
        var savedFile = await _fileService.SaveFile(Path.Combine(_equipementDocumentsPath, equipementDocumentDto.id_equipement.ToString()), equipementDocumentDto.document.FileName, equipementDocumentDto.document.ContentType, equipementDocumentDto.document.OpenReadStream());
        var equipementDocument = new EquipementsDocuments
        {
            id_equipement = equipementDocumentDto.id_equipement,
            url_equipement_document = savedFile.path,
            name_equipement_document = equipementDocumentDto.name_equipement_document,
            type_equipement_document = savedFile.mime_type,
            size_equipement_document = equipementDocumentDto.document.Length
        };
        await _context.EquipementsDocuments.AddAsync(equipementDocument);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadEquipementDocumentDto>(equipementDocument);
    }

    public async Task<ReadEquipementDocumentDto> UpdateEquipementDocument(int id, UpdateEquipementDocumentDto equipementDocumentDto, int? equipementId = null)
    {
        var equipementDocument = await _context.EquipementsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"EquipementDocument with id '{id}' not found");
        if (equipementId is not null && equipementDocument.id_equipement != equipementId)
        {
            throw new KeyNotFoundException($"EquipementDocument with id '{id}' not found for equipement with id '{equipementId}'");
        }
        if (equipementDocumentDto.name_equipement_document is not null)
        {
            equipementDocument.name_equipement_document = equipementDocumentDto.name_equipement_document;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadEquipementDocumentDto>(equipementDocument);
    }

    public async Task DeleteEquipementDocument(int id, int? equipementId = null)
    {
        var equipementDocument = await _context.EquipementsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"EquipementDocument with id '{id}' not found");
        if (equipementId is not null && equipementDocument.id_equipement != equipementId)
        {
            throw new KeyNotFoundException($"EquipementDocument with id '{id}' not found for equipement with id '{equipementId}'");
        }
        await _fileService.DeleteFile(equipementDocument.url_equipement_document);
        _context.EquipementsDocuments.Remove(equipementDocument);
        await _context.SaveChangesAsync();
    }
}
