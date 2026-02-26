using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Enums;
using electrostore.Extensions;
using electrostore.Models;
using electrostore.Services.SessionService;
using System.Linq.Expressions;

namespace electrostore.Services.StoreTagService;

public class StoreTagService : IStoreTagService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;

    public StoreTagService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
    }

    public async Task<PaginatedResponseDto<ReadExtendedStoreTagDto>> GetStoresTagsByStoreId(int storeId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeId))
        {
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
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == tagId))
        {
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
        var storeTag = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"StoreTag with storeId '{storeId}' and tagId '{tagId}' not found");
        return _mapper.Map<ReadExtendedStoreTagDto>(storeTag);
    }

    public async Task<ReadStoreTagDto> CreateStoreTag(CreateStoreTagDto storeTagDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to create StoreTag");
        }
        // check if store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeTagDto.id_store))
        {
            throw new KeyNotFoundException($"Store with id '{storeTagDto.id_store}' not found");
        }
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == storeTagDto.id_tag))
        {
            throw new KeyNotFoundException($"Tag with id '{storeTagDto.id_tag}' not found");
        }
        // check if store tag already exists
        if (await _context.StoresTags.AnyAsync(st => st.id_store == storeTagDto.id_store && st.id_tag == storeTagDto.id_tag))
        {
            throw new InvalidOperationException($"StoreTag with storeId '{storeTagDto.id_store}' and tagId '{storeTagDto.id_tag}' already exists");
        }
        var newStoreTag = _mapper.Map<StoresTags>(storeTagDto);
        _context.StoresTags.Add(newStoreTag);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadStoreTagDto>(newStoreTag);
    }

    public async Task<ReadBulkStoreTagDto> CreateBulkStoreTag(List<CreateStoreTagDto> storeTagBulkDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
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
            throw new UnauthorizedAccessException("You are not authorized to delete StoreTag");
        }
        var storeTag = await _context.StoresTags.FindAsync(storeId, tagId) ?? throw new KeyNotFoundException($"StoreTag with storeId '{storeId}' and tagId '{tagId}' not found");
        _context.StoresTags.Remove(storeTag);
        await _context.SaveChangesAsync();
    }

    public async Task<ReadBulkStoreTagDto> DeleteBulkStoreTag(List<CreateStoreTagDto> storeTagBulkDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
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
        return new ReadBulkStoreTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }
}