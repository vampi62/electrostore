using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Enums;
using electrostore.Services.SessionService;

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

    public async Task<IEnumerable<ReadExtendedStoreTagDto>> GetStoresTagsByStoreId(int storeId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeId))
        {
            throw new KeyNotFoundException($"Store with id {storeId} not found");
        }
        var query = _context.StoresTags.AsQueryable();
        query = query.Where(st => st.id_store == storeId);
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(st => st.id_tag);
        if (expand != null && expand.Contains("tag"))
        {
            query = query.Include(st => st.Tag);
        }
        if (expand != null && expand.Contains("store"))
        {
            query = query.Include(st => st.Store);
        }
        var storeTag = await query.ToListAsync();
        return _mapper.Map<List<ReadExtendedStoreTagDto>>(storeTag);
    }

    public async Task<int> GetStoresTagsCountByStoreId(int storeId)
    {
        // check if store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeId))
        {
            throw new KeyNotFoundException($"Store with id {storeId} not found");
        }
        return await _context.StoresTags
            .CountAsync(st => st.id_store == storeId);
    }

    public async Task<IEnumerable<ReadExtendedStoreTagDto>> GetStoresTagsByTagId(int tagId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == tagId))
        {
            throw new KeyNotFoundException($"Tag with id {tagId} not found");
        }
        var query = _context.StoresTags.AsQueryable();
        query = query.Where(st => st.id_tag == tagId);
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(st => st.id_store);
        if (expand != null && expand.Contains("tag"))
        {
            query = query.Include(st => st.Tag);
        }
        if (expand != null && expand.Contains("store"))
        {
            query = query.Include(st => st.Store);
        }
        var storeTag = await query.ToListAsync();
        return _mapper.Map<List<ReadExtendedStoreTagDto>>(storeTag);
    }

    public async Task<int> GetStoresTagsCountByTagId(int tagId)
    {
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == tagId))
        {
            throw new KeyNotFoundException($"Tag with id {tagId} not found");
        }
        return await _context.StoresTags
            .CountAsync(st => st.id_tag == tagId);
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
        var storeTag = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"StoreTag with storeId {storeId} and tagId {tagId} not found");
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
            throw new KeyNotFoundException($"Store with id {storeTagDto.id_store} not found");
        }
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == storeTagDto.id_tag))
        {
            throw new KeyNotFoundException($"Tag with id {storeTagDto.id_tag} not found");
        }
        // check if store tag already exists
        if (await _context.StoresTags.AnyAsync(st => st.id_store == storeTagDto.id_store && st.id_tag == storeTagDto.id_tag))
        {
            throw new InvalidOperationException($"StoreTag with storeId {storeTagDto.id_store} and tagId {storeTagDto.id_tag} already exists");
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
        var storeTag = await _context.StoresTags.FindAsync(storeId, tagId) ?? throw new KeyNotFoundException($"StoreTag with storeId {storeId} and tagId {tagId} not found");
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