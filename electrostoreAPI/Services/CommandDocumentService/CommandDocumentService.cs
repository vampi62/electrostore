using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Extensions;
using electrostore.Models;
using electrostore.Services.FileService;
using System.Linq.Expressions;

namespace electrostore.Services.CommandDocumentService;

public class CommandDocumentService : ICommandDocumentService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly string _commandDocumentsPath = "commandDocuments";

    public CommandDocumentService(IMapper mapper, ApplicationDbContext context, IFileService fileService)
    {
        _mapper = mapper;
        _context = context;
        _fileService = fileService;
    }

    public async Task<PaginatedResponseDto<ReadCommandDocumentDto>> GetCommandsDocumentsByCommandId(int commandId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null)
    {
        // check if command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandId))
        {
            throw new KeyNotFoundException($"Command with id '{commandId}' not found");
        }
        var query = _context.CommandsDocuments.AsQueryable();
        var filterResult = default(Expression<Func<CommandsDocuments, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_command", SearchType = "eq", Value = commandId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<CommandsDocuments>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<CommandsDocuments>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_command_document", Order = "asc" };
                query = query.OrderBy(cd => cd.id_command_document);
            }
        }
        else
        {
            query = query.OrderBy(cd => cd.id_command_document);
        }
        query = query.Skip(offset).Take(limit);
        var commandDocument = await query.ToListAsync();
        return new PaginatedResponseDto<ReadCommandDocumentDto>
        {
            data = _mapper.Map<IEnumerable<ReadCommandDocumentDto>>(commandDocument),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.CommandsDocuments.CountAsync(filterResult ?? (cd => cd.id_command == commandId)),
                nextOffset = offset + limit,
                hasMore = await _context.CommandsDocuments.Skip(offset + limit).AnyAsync(filterResult ?? (cd => cd.id_command == commandId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadCommandDocumentDto> GetCommandDocumentById(int id, int? commandId = null)
    {
        var commandDocument = await _context.CommandsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"CommandDocument with id '{id}' not found");
        if (commandId is not null && commandDocument.id_command != commandId)
        {
            throw new KeyNotFoundException($"CommandDocument with id '{id}' not found for command with id '{commandId}'");
        }
        return _mapper.Map<ReadCommandDocumentDto>(commandDocument);
    }

    public async Task<ReadCommandDocumentDto> CreateCommandDocument(CreateCommandDocumentDto commandDocumentDto)
    {
        // check if command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandDocumentDto.id_command))
        {
            throw new KeyNotFoundException($"Command with id '{commandDocumentDto.id_command}' not found");
        }
        var savedFile = await _fileService.SaveFile(Path.Combine(_commandDocumentsPath, commandDocumentDto.id_command.ToString()), commandDocumentDto.document);
        var commandDocument = new CommandsDocuments
        {
            id_command = commandDocumentDto.id_command,
            url_command_document = savedFile.url,
            name_command_document = commandDocumentDto.name_command_document,
            type_command_document = savedFile.mimeType,
            size_command_document = commandDocumentDto.document.Length
        };
        await _context.CommandsDocuments.AddAsync(commandDocument);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCommandDocumentDto>(commandDocument);
    }

    public async Task<ReadCommandDocumentDto> UpdateCommandDocument(int id, UpdateCommandDocumentDto commandDocumentDto, int? commandId = null)
    {
        var commandDocument = await _context.CommandsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"CommandDocument with id '{id}' not found");
        if (commandId is not null && commandDocument.id_command != commandId)
        {
            throw new KeyNotFoundException($"CommandDocument with id '{id}' not found for command with id '{commandId}'");
        }
        if (commandDocumentDto.name_command_document is not null)
        {
            commandDocument.name_command_document = commandDocumentDto.name_command_document;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCommandDocumentDto>(commandDocument);
    }

    public async Task DeleteCommandDocument(int id, int? commandId = null)
    {
        var commandDocument = await _context.CommandsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"CommandDocument with id '{id}' not found");
        if (commandId is not null && commandDocument.id_command != commandId)
        {
            throw new KeyNotFoundException($"CommandDocument with id '{id}' not found for command with id '{commandId}'");
        }
        await _fileService.DeleteFile(commandDocument.url_command_document);
        _context.CommandsDocuments.Remove(commandDocument);
        await _context.SaveChangesAsync();
    }
}