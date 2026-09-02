using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.TagService;

public class TagService : ITagService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public TagService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<PaginatedResponseDto<ReadExtendedTagDto>> GetTags(int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Tags.AsQueryable();
        var filterResult = default(Expression<Func<Tags, bool>>);
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(t => idResearch.Contains(t.id_tag));
        }
        else
        {
            if (rsql != null && rsql.Count > 0)
            {
                (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<Tags>(rsql);
                query = query.Where(filterResult);
            }
            if (!string.IsNullOrEmpty(sort?.field))
            {
                var sortResult = RsqlParserExtensions.ToSortExpression<Tags>(sort);
                if (sortResult.Item1 != null)
                {
                    query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
                }
                else
                {
                    sort = new SorterDto { field = "id_tag", order = "asc" };
                    query = query.OrderBy(t => t.id_tag);
                }
            }
            else
            {
                query = query.OrderBy(t => t.id_tag);
            }
        }
        query = query.Skip(offset).Take(limit);
        var tags = await query
            .Select(t => new
            {
                Tag = t,
                StoresTagsCount = t.StoresTags.Count,
                ItemsTagsCount = t.ItemsTags.Count,
                BoxsTagsCount = t.BoxsTags.Count,
                StoresTags = expand != null && expand.Contains("stores_tags") ? t.StoresTags.Take(20).ToList() : null,
                ItemsTags = expand != null && expand.Contains("items_tags") ? t.ItemsTags.Take(20).ToList() : null,
                BoxsTags = expand != null && expand.Contains("boxs_tags") ? t.BoxsTags.Take(20).ToList() : null
            })
            .ToListAsync();
        return new PaginatedResponseDto<ReadExtendedTagDto>
        {
            data = tags.Select(t => {
                return _mapper.Map<ReadExtendedTagDto>(t.Tag) with
                {
                    stores_tags_count = t.StoresTagsCount,
                    items_tags_count = t.ItemsTagsCount,
                    boxs_tags_count = t.BoxsTagsCount,
                    stores_tags = _mapper.Map<IEnumerable<ReadStoreTagDto>>(t.StoresTags),
                    items_tags = _mapper.Map<IEnumerable<ReadItemTagDto>>(t.ItemsTags),
                    boxs_tags = _mapper.Map<IEnumerable<ReadBoxTagDto>>(t.BoxsTags)
                };
            }).ToList(),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.Tags.CountAsync(filterResult ?? (t => true)),
                next_offset = offset + limit,
                has_more = await _context.Tags.Skip(offset + limit).AnyAsync(filterResult ?? (t => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedTagDto> GetTagById(int id, List<string>? expand = null)
    {
        var query = _context.Tags.AsQueryable();
        query = query.Where(t => t.id_tag == id);
        var tag = await query
            .Select(t => new
            {
                Tag = t,
                StoresTagsCount = t.StoresTags.Count,
                ItemsTagsCount = t.ItemsTags.Count,
                BoxsTagsCount = t.BoxsTags.Count,
                StoresTags = expand != null && expand.Contains("stores_tags") ? t.StoresTags.Take(20).ToList() : null,
                ItemsTags = expand != null && expand.Contains("items_tags") ? t.ItemsTags.Take(20).ToList() : null,
                BoxsTags = expand != null && expand.Contains("boxs_tags") ? t.BoxsTags.Take(20).ToList() : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Tag with id '{id}' not found");
        return _mapper.Map<ReadExtendedTagDto>(tag.Tag) with
        {
            stores_tags_count = tag.StoresTagsCount,
            items_tags_count = tag.ItemsTagsCount,
            boxs_tags_count = tag.BoxsTagsCount,
            stores_tags = _mapper.Map<IEnumerable<ReadStoreTagDto>>(tag.StoresTags),
            items_tags = _mapper.Map<IEnumerable<ReadItemTagDto>>(tag.ItemsTags),
            boxs_tags = _mapper.Map<IEnumerable<ReadBoxTagDto>>(tag.BoxsTags)
        };
    }

    public async Task<ReadTagDto> CreateTag(CreateTagDto tagDto)
    {
        // check if tag name already exists
        if (await _context.Tags.AnyAsync(t => t.name_tag == tagDto.name_tag))
        {
            throw new InvalidOperationException($"Tag with name '{tagDto.name_tag}' already exists");
        }
        var newTag = _mapper.Map<Tags>(tagDto);
        _context.Tags.Add(newTag);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadTagDto>(newTag);
    }

    public async Task<ReadBulkTagDto> CreateBulkTag(List<CreateTagDto> tagBulkDto)
    {
        var validQuery = new List<ReadTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var tagDto in tagBulkDto)
        {
            try
            {
                validQuery.Add(await CreateTag(tagDto));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    reason = e.Message,
                    data = tagDto
                });
            }
        }
        return new ReadBulkTagDto
        {
            valide = validQuery,
            error = errorQuery
        };
    }

    public async Task<ReadTagDto> UpdateTag(int id, UpdateTagDto tagDto)
    {
        var tagToUpdate = await _context.Tags.FindAsync(id) ?? throw new KeyNotFoundException($"Tag with id '{id}' not found");
        if (tagDto.name_tag is not null)
        {
            // check if another tag with the name already exists
            if (await _context.Tags.AnyAsync(t => t.name_tag == tagDto.name_tag && t.id_tag != id))
            {
                throw new InvalidOperationException($"Tag with name '{tagDto.name_tag}' already exists");
            }
            tagToUpdate.name_tag = tagDto.name_tag;
        }
        if (tagDto.weight_tag is not null)
        {
            tagToUpdate.weight_tag = tagDto.weight_tag.Value;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadTagDto>(tagToUpdate);
    }

    public async Task DeleteTag(int id)
    {
        var tagToDelete = await _context.Tags.FindAsync(id) ?? throw new KeyNotFoundException($"Tag with id '{id}' not found");
        _context.Tags.Remove(tagToDelete);
        await _context.SaveChangesAsync();
    }
}