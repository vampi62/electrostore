using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.SessionService;
using Microsoft.EntityFrameworkCore;

namespace ElectrostoreAPI.Services.StoreTagService;

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
        rsql ??= [];
        rsql.Add(new FilterDto { field = "id_store", search_type = "eq", value = storeId.ToString() });
        if (expand != null && expand.Contains("tag"))
        {
            query = query.Include(st => st.Tag);
        }
        if (expand != null && expand.Contains("store"))
        {
            query = query.Include(st => st.Store);
        }
        return await query
            .ToPagedQuery(limit, offset, rsql, sort, new SorterDto { field = "id_tag", order = "asc" })
            .ToResponseAsync(storeTag => _mapper.Map<List<ReadExtendedStoreTagDto>>(storeTag));
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
        rsql ??= [];
        rsql.Add(new FilterDto { field = "id_tag", search_type = "eq", value = tagId.ToString() });
        if (expand != null && expand.Contains("tag"))
        {
            query = query.Include(st => st.Tag);
        }
        if (expand != null && expand.Contains("store"))
        {
            query = query.Include(st => st.Store);
        }
        return await query
            .ToPagedQuery(limit, offset, rsql, sort, new SorterDto { field = "id_store", order = "asc" })
            .ToResponseAsync(storeTag => _mapper.Map<List<ReadExtendedStoreTagDto>>(storeTag));
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

    public async Task<ReadBulkDto<ReadStoreTagDto>> CreateBulkStoreTag(List<CreateStoreTagDto> storeTagBulkDto)
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
                    reason = e.Message,
                    data = storeTagDto
                });
            }
        }
        return new ReadBulkDto<ReadStoreTagDto>
        {
            valide = validQuery,
            error = errorQuery
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

    public async Task<ReadBulkDto<ReadStoreTagDto>> DeleteBulkStoreTag(List<CreateStoreTagDto> storeTagBulkDto)
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
                    reason = e.Message,
                    data = storeTagDto
                });
            }
        }
        await _context.SaveChangesAsync();
        return new ReadBulkDto<ReadStoreTagDto>
        {
            valide = validQuery,
            error = errorQuery
        };
    }
}