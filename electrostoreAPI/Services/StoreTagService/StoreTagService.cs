using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.SessionService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.StoreTagService;

public class StoreTagService : IStoreTagService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;
    private readonly ILogger<StoreTagService> _logger;

    public StoreTagService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService, ILogger<StoreTagService> logger)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
        _logger = logger;
    }

    public async Task<PaginatedResponseDto<ReadExtendedStoreTagDto>> GetStoresTagsByStoreId(int storeId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        _logger.LogDebug("GetStoresTagsByStoreId: storeId={StoreId}, limit={Limit}, offset={Offset}", storeId, limit, offset);
        // check if store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeId))
        {
            _logger.LogWarning("GetStoresTagsByStoreId: Store {StoreId} not found", storeId);
            throw new KeyNotFoundException($"Store with id '{storeId}' not found");
        }
        var query = _context.StoresTags.AsQueryable();
        var filterResult = default(Expression<Func<StoresTags, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_store", SearchType = "eq", Value = storeId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<StoresTags>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<StoresTags>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_tag", Order = "asc" };
                query = query.OrderBy(st => st.id_tag);
            }
        }
        else
        {
            query = query.OrderBy(st => st.id_tag);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("tag"))
        {
            query = query.Include(st => st.Tag);
        }
        if (expand != null && expand.Contains("store"))
        {
            query = query.Include(st => st.Store);
        }
        var storeTag = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedStoreTagDto>
        {
            data = _mapper.Map<List<ReadExtendedStoreTagDto>>(storeTag),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.StoresTags.CountAsync(filterResult ?? ( st => st.id_store == storeId)),
                nextOffset = offset + limit,
                hasMore = await _context.StoresTags.Skip(offset + limit).AnyAsync(filterResult ?? (st => st.id_store == storeId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<PaginatedResponseDto<ReadExtendedStoreTagDto>> GetStoresTagsByTagId(int tagId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        _logger.LogDebug("GetStoresTagsByTagId: tagId={TagId}, limit={Limit}, offset={Offset}", tagId, limit, offset);
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == tagId))
        {
            _logger.LogWarning("GetStoresTagsByTagId: Tag {TagId} not found", tagId);
            throw new KeyNotFoundException($"Tag with id '{tagId}' not found");
        }
        var query = _context.StoresTags.AsQueryable();
        var filterResult = default(Expression<Func<StoresTags, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_tag", SearchType = "eq", Value = tagId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<StoresTags>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<StoresTags>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_store", Order = "asc" };
                query = query.OrderBy(st => st.id_store);
            }
        }
        else
        {
            query = query.OrderBy(st => st.id_store);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("tag"))
        {
            query = query.Include(st => st.Tag);
        }
        if (expand != null && expand.Contains("store"))
        {
            query = query.Include(st => st.Store);
        }
        var storeTag = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedStoreTagDto>
        {
            data = _mapper.Map<List<ReadExtendedStoreTagDto>>(storeTag),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.StoresTags.CountAsync(filterResult ?? (st => st.id_tag == tagId)),
                nextOffset = offset + limit,
                hasMore = await _context.StoresTags.Skip(offset + limit).AnyAsync(filterResult ?? (st => st.id_tag == tagId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedStoreTagDto> GetStoreTagById(int storeId, int tagId, List<string>? expand = null)
    {
        var query = _context.StoresTags.AsQueryable();
        query = query.Where(st => st.id_store == storeId && st.id_tag == tagId);
        if (expand != null && expand.Contains("tag"))
        {
            query = query.Include(st => st.Tag);
        }
        if (expand != null && expand.Contains("store"))
        {
            query = query.Include(st => st.Store);
        }
        var storeTag = await query.FirstOrDefaultAsync();
        if (storeTag is null)
        {
            _logger.LogWarning("GetStoreTagById: StoreTag with storeId {StoreId} and tagId {TagId} not found", storeId, tagId);
            throw new KeyNotFoundException($"StoreTag with storeId '{storeId}' and tagId '{tagId}' not found");
        }
        return _mapper.Map<ReadExtendedStoreTagDto>(storeTag);
    }

    public async Task<ReadStoreTagDto> CreateStoreTag(CreateStoreTagDto storeTagDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            _logger.LogWarning("CreateStoreTag: unauthorized attempt to create StoreTag for storeId {StoreId} and tagId {TagId}", storeTagDto.id_store, storeTagDto.id_tag);
            throw new UnauthorizedAccessException("You are not authorized to create StoreTag");
        }
        // check if store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeTagDto.id_store))
        {
            _logger.LogWarning("CreateStoreTag: Store {StoreId} not found", storeTagDto.id_store);
            throw new KeyNotFoundException($"Store with id '{storeTagDto.id_store}' not found");
        }
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == storeTagDto.id_tag))
        {
            _logger.LogWarning("CreateStoreTag: Tag {TagId} not found", storeTagDto.id_tag);
            throw new KeyNotFoundException($"Tag with id '{storeTagDto.id_tag}' not found");
        }
        // check if store tag already exists
        if (await _context.StoresTags.AnyAsync(st => st.id_store == storeTagDto.id_store && st.id_tag == storeTagDto.id_tag))
        {
            _logger.LogWarning("CreateStoreTag: StoreTag with storeId {StoreId} and tagId {TagId} already exists", storeTagDto.id_store, storeTagDto.id_tag);
            throw new InvalidOperationException($"StoreTag with storeId '{storeTagDto.id_store}' and tagId '{storeTagDto.id_tag}' already exists");
        }
        var newStoreTag = _mapper.Map<StoresTags>(storeTagDto);
        _context.StoresTags.Add(newStoreTag);
        await _context.SaveChangesAsync();
        _logger.LogInformation("CreateStoreTag: StoreTag created for storeId {StoreId} and tagId {TagId}", newStoreTag.id_store, newStoreTag.id_tag);
        return _mapper.Map<ReadStoreTagDto>(newStoreTag);
    }

    public async Task<ReadBulkStoreTagDto> CreateBulkStoreTag(List<CreateStoreTagDto> storeTagBulkDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            _logger.LogWarning("CreateBulkStoreTag: unauthorized attempt to create {Count} StoreTags", storeTagBulkDto.Count);
            throw new UnauthorizedAccessException("You are not authorized to create StoreTag");
        }
        var validQuery = new List<ReadStoreTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var storeTagDto in storeTagBulkDto)
        {
            try
            {
                validQuery.Add(await CreateStoreTag(storeTagDto));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = storeTagDto
                });
            }
        }
        _logger.LogInformation("CreateBulkStoreTag: {ValidCount} created, {ErrorCount} failed", validQuery.Count, errorQuery.Count);
        return new ReadBulkStoreTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task DeleteStoreTag(int storeId, int tagId)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            _logger.LogWarning("DeleteStoreTag: unauthorized attempt to delete StoreTag for storeId {StoreId} and tagId {TagId}", storeId, tagId);
            throw new UnauthorizedAccessException("You are not authorized to delete StoreTag");
        }
        var storeTag = await _context.StoresTags.FindAsync(storeId, tagId);
        if (storeTag is null)
        {
            _logger.LogWarning("DeleteStoreTag: StoreTag with storeId {StoreId} and tagId {TagId} not found", storeId, tagId);
            throw new KeyNotFoundException($"StoreTag with storeId '{storeId}' and tagId '{tagId}' not found");
        }
        _context.StoresTags.Remove(storeTag);
        await _context.SaveChangesAsync();
        _logger.LogInformation("DeleteStoreTag: StoreTag deleted for storeId {StoreId} and tagId {TagId}", storeId, tagId);
    }

    public async Task<ReadBulkStoreTagDto> DeleteBulkStoreTag(List<CreateStoreTagDto> storeTagBulkDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            _logger.LogWarning("DeleteBulkStoreTag: unauthorized attempt to delete {Count} StoreTags", storeTagBulkDto.Count);
            throw new UnauthorizedAccessException("You are not authorized to delete StoreTag");
        }
        var validQuery = new List<ReadStoreTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var storeTagDto in storeTagBulkDto)
        {
            try
            {
                await DeleteStoreTag(storeTagDto.id_store, storeTagDto.id_tag);
                validQuery.Add(new ReadStoreTagDto
                {
                    id_store = storeTagDto.id_store,
                    id_tag = storeTagDto.id_tag
                });
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = storeTagDto
                });
            }
        }
        await _context.SaveChangesAsync();
        _logger.LogInformation("DeleteBulkStoreTag: {ValidCount} deleted, {ErrorCount} failed", validQuery.Count, errorQuery.Count);
        return new ReadBulkStoreTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }
}