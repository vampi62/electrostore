using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Enums;
using electrostore.Services.SessionService;

namespace electrostore.Services.BoxTagService;

public class BoxTagService : IBoxTagService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;

    public BoxTagService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
    }

    public async Task<IEnumerable<ReadExtendedBoxTagDto>> GetBoxsTagsByBoxId(int boxId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if box exists
        if (!await _context.Boxs.AnyAsync(b => b.id_box == boxId))
        {
            throw new KeyNotFoundException($"Box with id {boxId} not found");
        }
        var query = _context.BoxsTags.AsQueryable();
        query = query.Where(bt => bt.id_box == boxId);
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(bt => bt.id_tag);
        if (expand != null && expand.Contains("tag"))
        {
            query = query.Include(bt => bt.Tag);
        }
        if (expand != null && expand.Contains("box"))
        {
            query = query.Include(bt => bt.Box);
        }
        var boxTag = await query.ToListAsync();
        return _mapper.Map<IEnumerable<ReadExtendedBoxTagDto>>(boxTag);
    }

    public async Task<int> GetBoxsTagsCountByBoxId(int boxId)
    {
        // check if box exists
        if (!await _context.Boxs.AnyAsync(b => b.id_box == boxId))
        {
            throw new KeyNotFoundException($"Box with id {boxId} not found");
        }
        return await _context.BoxsTags
            .Where(bt => bt.id_box == boxId)
            .CountAsync();
    }

    public async Task<IEnumerable<ReadExtendedBoxTagDto>> GetBoxsTagsByTagId(int tagId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == tagId))
        {
            throw new KeyNotFoundException($"Tag with id {tagId} not found");
        }
        var query = _context.BoxsTags.AsQueryable();
        query = query.Where(bt => bt.id_tag == tagId);
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(bt => bt.id_box);
        if (expand != null && expand.Contains("tag"))
        {
            query = query.Include(bt => bt.Tag);
        }
        if (expand != null && expand.Contains("box"))
        {
            query = query.Include(bt => bt.Box);
        }
        var boxTag = await query.ToListAsync();
        return _mapper.Map<IEnumerable<ReadExtendedBoxTagDto>>(boxTag);
    }

    public async Task<int> GetBoxsTagsCountByTagId(int tagId)
    {
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == tagId))
        {
            throw new KeyNotFoundException($"Tag with id {tagId} not found");
        }
        return await _context.BoxsTags
            .Where(s => s.id_tag == tagId)
            .CountAsync();
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
        var boxTag = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"BoxTag with id {boxId} and {tagId} not found");
        return _mapper.Map<ReadExtendedBoxTagDto>(boxTag);
    }

    public async Task<ReadBoxTagDto> CreateBoxTag(CreateBoxTagDto boxTagDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to create BoxTag");
        }
        // check if box exists
        if (!await _context.Boxs.AnyAsync(b => b.id_box == boxTagDto.id_box))
        {
            throw new KeyNotFoundException($"Box with id {boxTagDto.id_box} not found");
        }
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == boxTagDto.id_tag))
        {
            throw new KeyNotFoundException($"Tag with id {boxTagDto.id_tag} not found");
        }
        // check if the boxtag already exists
        if (await _context.BoxsTags.AnyAsync(bt => bt.id_box == boxTagDto.id_box && bt.id_tag == boxTagDto.id_tag))
        {
            throw new InvalidOperationException($"BoxTag with id {boxTagDto.id_box} and {boxTagDto.id_tag} already exists");
        }
        var newBoxTag = _mapper.Map<BoxsTags>(boxTagDto);
        _context.BoxsTags.Add(newBoxTag);
        await _context.SaveChangesAsync();
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
            throw new UnauthorizedAccessException("You are not authorized to delete BoxTag");
        }
        var boxTagToDelete = await _context.BoxsTags.FindAsync(boxId, tagId) ?? throw new KeyNotFoundException($"BoxTag with id {boxId} and {tagId} not found");
        _context.BoxsTags.Remove(boxTagToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task CheckIfStoreExists(int storeId, int boxId)
    {
        if (!await _context.Boxs.AnyAsync(b => b.id_box == boxId && b.id_store == storeId))
        {
            throw new KeyNotFoundException($"Box with id {boxId} not found in store with id {storeId}");
        }
    }

    public async Task<ReadBulkBoxTagDto> DeleteBulkItemTag(List<CreateBoxTagDto> boxTagBulkDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
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
        return new ReadBulkBoxTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }
}