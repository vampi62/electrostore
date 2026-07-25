using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.SessionService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.BoxTagService;

public class BoxTagService : IBoxTagService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;
    private readonly ILogger<BoxTagService> _logger;

    public BoxTagService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService, ILogger<BoxTagService> logger)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
        _logger = logger;
    }

    public async Task<PaginatedResponseDto<ReadExtendedBoxTagDto>> GetBoxsTagsByBoxId(int boxId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        _logger.LogDebug("GetBoxsTagsByBoxId: boxId {BoxId}, limit {Limit}, offset {Offset}", boxId, limit, offset);
        // check if box exists
        if (!await _context.Boxs.AnyAsync(b => b.id_box == boxId))
        {
            _logger.LogWarning("GetBoxsTagsByBoxId: box {BoxId} not found", boxId);
            throw new KeyNotFoundException($"Box with id '{boxId}' not found");
        }
        var query = _context.BoxsTags.AsQueryable();
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_box", SearchType = "eq", Value = boxId.ToString() });
        var filterResult = default(Expression<Func<BoxsTags, bool>>);
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<BoxsTags>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<BoxsTags>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_tag", Order = "asc" };
                query = query.OrderBy(bt => bt.id_tag);
            }
        }
        else
        {
            query = query.OrderBy(bt => bt.id_tag);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("tag"))
        {
            query = query.Include(bt => bt.Tag);
        }
        if (expand != null && expand.Contains("box"))
        {
            query = query.Include(bt => bt.Box);
        }
        var boxTag = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedBoxTagDto>
        {
            data = _mapper.Map<IEnumerable<ReadExtendedBoxTagDto>>(boxTag),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.BoxsTags.CountAsync(filterResult ?? (bt => true)),
                nextOffset = offset + limit,
                hasMore = await _context.BoxsTags.Skip(offset + limit).AnyAsync(filterResult ?? (bt => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<PaginatedResponseDto<ReadExtendedBoxTagDto>> GetBoxsTagsByTagId(int tagId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        _logger.LogDebug("GetBoxsTagsByTagId: tagId {TagId}, limit {Limit}, offset {Offset}", tagId, limit, offset);
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == tagId))
        {
            _logger.LogWarning("GetBoxsTagsByTagId: tag {TagId} not found", tagId);
            throw new KeyNotFoundException($"Tag with id '{tagId}' not found");
        }
        var query = _context.BoxsTags.AsQueryable();
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_tag", SearchType = "eq", Value = tagId.ToString() });
        var filterResult = default(Expression<Func<BoxsTags, bool>>);
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<BoxsTags>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<BoxsTags>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_box", Order = "asc" };
                query = query.OrderBy(bt => bt.id_box);
            }
        }
        else
        {
            query = query.OrderBy(bt => bt.id_box);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("tag"))
        {
            query = query.Include(bt => bt.Tag);
        }
        if (expand != null && expand.Contains("box"))
        {
            query = query.Include(bt => bt.Box);
        }
        var boxTag = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedBoxTagDto>
        {
            data = _mapper.Map<IEnumerable<ReadExtendedBoxTagDto>>(boxTag),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.BoxsTags.CountAsync(filterResult ?? (bt => true)),
                nextOffset = offset + limit,
                hasMore = await _context.BoxsTags.Skip(offset + limit).AnyAsync(filterResult ?? (bt => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedBoxTagDto> GetBoxTagById(int boxId, int tagId, List<string>? expand = null)
    {
        var query = _context.BoxsTags.AsQueryable();
        query = query.Where(bt => bt.id_box == boxId && bt.id_tag == tagId);
        if (expand != null && expand.Contains("tag"))
        {
            query = query.Include(bt => bt.Tag);
        }
        if (expand != null && expand.Contains("box"))
        {
            query = query.Include(bt => bt.Box);
        }
        var boxTag = await query.FirstOrDefaultAsync();
        if (boxTag is null)
        {
            _logger.LogWarning("GetBoxTagById: boxtag with box {BoxId} and tag {TagId} not found", boxId, tagId);
            throw new KeyNotFoundException($"BoxTag with id '{boxId}' and '{tagId}' not found");
        }
        return _mapper.Map<ReadExtendedBoxTagDto>(boxTag);
    }

    public async Task<ReadBoxTagDto> CreateBoxTag(CreateBoxTagDto boxTagDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            _logger.LogWarning("CreateBoxTag: unauthorized attempt to create BoxTag (role {ClientRole})", clientRole);
            throw new UnauthorizedAccessException("You are not authorized to create BoxTag");
        }
        // check if box exists
        if (!await _context.Boxs.AnyAsync(b => b.id_box == boxTagDto.id_box))
        {
            _logger.LogWarning("CreateBoxTag: box {BoxId} not found", boxTagDto.id_box);
            throw new KeyNotFoundException($"Box with id '{boxTagDto.id_box}' not found");
        }
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == boxTagDto.id_tag))
        {
            _logger.LogWarning("CreateBoxTag: tag {TagId} not found", boxTagDto.id_tag);
            throw new KeyNotFoundException($"Tag with id '{boxTagDto.id_tag}' not found");
        }
        // check if the boxtag already exists
        if (await _context.BoxsTags.AnyAsync(bt => bt.id_box == boxTagDto.id_box && bt.id_tag == boxTagDto.id_tag))
        {
            _logger.LogWarning("CreateBoxTag: boxtag with box {BoxId} and tag {TagId} already exists", boxTagDto.id_box, boxTagDto.id_tag);
            throw new InvalidOperationException($"BoxTag with id '{boxTagDto.id_box}' and '{boxTagDto.id_tag}' already exists");
        }
        var newBoxTag = _mapper.Map<BoxsTags>(boxTagDto);
        _context.BoxsTags.Add(newBoxTag);
        await _context.SaveChangesAsync();
        _logger.LogInformation("CreateBoxTag: boxtag created with box {BoxId} and tag {TagId}", newBoxTag.id_box, newBoxTag.id_tag);
        return _mapper.Map<ReadBoxTagDto>(newBoxTag);
    }

    public async Task<ReadBulkBoxTagDto> CreateBulkBoxTag(List<CreateBoxTagDto> boxTagBulkDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to create BoxTag");
        }
        var validQuery = new List<ReadBoxTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var boxTagDto in boxTagBulkDto)
        {
            try
            {
                validQuery.Add(await CreateBoxTag(boxTagDto));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = boxTagDto
                });
            }
        }
        _logger.LogInformation("CreateBulkBoxTag: {ValidCount} boxtags created, {ErrorCount} errors", validQuery.Count, errorQuery.Count);
        return new ReadBulkBoxTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task DeleteBoxTag(int boxId, int tagId)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            _logger.LogWarning("DeleteBoxTag: unauthorized attempt to delete BoxTag (role {ClientRole})", clientRole);
            throw new UnauthorizedAccessException("You are not authorized to delete BoxTag");
        }
        var boxTagToDelete = await _context.BoxsTags.FindAsync(boxId, tagId);
        if (boxTagToDelete is null)
        {
            _logger.LogWarning("DeleteBoxTag: boxtag with box {BoxId} and tag {TagId} not found", boxId, tagId);
            throw new KeyNotFoundException($"BoxTag with id '{boxId}' and '{tagId}' not found");
        }
        _context.BoxsTags.Remove(boxTagToDelete);
        await _context.SaveChangesAsync();
        _logger.LogInformation("DeleteBoxTag: boxtag with box {BoxId} and tag {TagId} deleted", boxId, tagId);
    }

    public async Task CheckIfStoreExists(int storeId, int boxId)
    {
        if (!await _context.Boxs.AnyAsync(b => b.id_box == boxId && b.id_store == storeId))
        {
            _logger.LogWarning("CheckIfStoreExists: box {BoxId} not found in store {StoreId}", boxId, storeId);
            throw new KeyNotFoundException($"Box with id '{boxId}' not found in store with id '{storeId}'");
        }
    }

    public async Task<ReadBulkBoxTagDto> DeleteBulkBoxTag(List<CreateBoxTagDto> boxTagBulkDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            _logger.LogWarning("DeleteBulkBoxTag: unauthorized attempt to delete BoxTag (role {ClientRole})", clientRole);
            throw new UnauthorizedAccessException("You are not authorized to delete BoxTag");
        }
        var validQuery = new List<ReadBoxTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var boxTagDto in boxTagBulkDto)
        {
            try
            {
                await DeleteBoxTag(boxTagDto.id_box, boxTagDto.id_tag);
                validQuery.Add(new ReadBoxTagDto
                {
                    id_box = boxTagDto.id_box,
                    id_tag = boxTagDto.id_tag
                });
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = boxTagDto
                });
            }
        }
        _logger.LogInformation("DeleteBulkBoxTag: {ValidCount} boxtags deleted, {ErrorCount} errors", validQuery.Count, errorQuery.Count);
        return new ReadBulkBoxTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }
}