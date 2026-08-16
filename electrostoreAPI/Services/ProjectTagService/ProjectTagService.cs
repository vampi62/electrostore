using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.ProjectTagService;

public class ProjectTagService : IProjectTagService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public ProjectTagService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<PaginatedResponseDto<ReadExtendedProjectTagDto>> GetProjectTags(int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.ProjectTags.AsQueryable();
        var filterResult = default(Expression<Func<ProjectTags, bool>>);
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(t => idResearch.Contains(t.id_project_tag));
        }
        else
        {
            if (rsql != null && rsql.Count > 0)
            {
                (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ProjectTags>(rsql);
                query = query.Where(filterResult);
            }
            if (!string.IsNullOrEmpty(sort?.Field))
            {
                var sortResult = RsqlParserExtensions.ToSortExpression<ProjectTags>(sort);
                if (sortResult.Item1 != null)
                {
                    query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
                }
                else
                {
                    sort = new SorterDto { Field = "id_project_tag", Order = "asc" };
                    query = query.OrderBy(t => t.id_project_tag);
                }
            }
            else
            {
                query = query.OrderBy(t => t.id_project_tag);
            }
        }
        query = query.Skip(offset).Take(limit);
        var projectTag = await query
            .Select(t => new
            {
                ProjectTags = t,
                ProjectsProjectTagsCount = t.ProjectsProjectTags.Count,
                ProjectsProjectTags = expand != null && expand.Contains("project_tags") ? t.ProjectsProjectTags.Take(20).ToList() : null
            })
            .ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjectTagDto>
        {
            data = projectTag.Select(t => _mapper.Map<ReadExtendedProjectTagDto>(t.ProjectTags) with
            {
                project_tags_count = t.ProjectsProjectTagsCount,
                project_tags = _mapper.Map<IEnumerable<ReadProjectProjectTagDto>>(t.ProjectsProjectTags)
            }).ToList(),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjectTags.CountAsync(filterResult ?? (t => true)),
                nextOffset = offset + limit,
                hasMore = await _context.ProjectTags.Skip(offset + limit).AnyAsync(filterResult ?? (t => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedProjectTagDto> GetProjectTagById(int id, List<string>? expand = null)
    {
        var query = _context.ProjectTags.AsQueryable();
        query = query.Where(t => t.id_project_tag == id);
        var projectTag = await query
            .Select(t => new
            {
                ProjectTags = t,
                ProjectsProjectTagsCount = t.ProjectsProjectTags.Count,
                ProjectsProjectTags = expand != null && expand.Contains("project_tags") ? t.ProjectsProjectTags.Take(20).ToList() : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"ProjectTag with id '{id}' not found");
        return _mapper.Map<ReadExtendedProjectTagDto>(projectTag.ProjectTags) with
        {
            project_tags_count = projectTag.ProjectsProjectTagsCount,
            project_tags = _mapper.Map<IEnumerable<ReadProjectProjectTagDto>>(projectTag.ProjectsProjectTags)
        };
    }

    public async Task<ReadProjectTagDto> CreateProjectTag(CreateProjectTagDto projectTagDto)
    {
        // check if tag name already exists
        if (await _context.ProjectTags.AnyAsync(t => t.name_project_tag == projectTagDto.name_project_tag))
        {
            throw new InvalidOperationException($"ProjectTag with name '{projectTagDto.name_project_tag}' already exists");
        }
        var newProjectTag = _mapper.Map<ProjectTags>(projectTagDto);
        _context.ProjectTags.Add(newProjectTag);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjectTagDto>(newProjectTag);
    }

    public async Task<ReadBulkProjectTagDto> CreateBulkProjectTag(List<CreateProjectTagDto> projectTagBulkDto)
    {
        var validQuery = new List<ReadProjectTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var projectTagDto in projectTagBulkDto)
        {
            try
            {
                validQuery.Add(await CreateProjectTag(projectTagDto));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = projectTagDto
                });
            }
        }
        return new ReadBulkProjectTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task<ReadProjectTagDto> UpdateProjectTag(int id, UpdateProjectTagDto projectTagDto)
    {
        var projectTagToUpdate = await _context.ProjectTags.FindAsync(id) ?? throw new KeyNotFoundException($"ProjectTag with id {id} not found");
        if (projectTagDto.name_project_tag is not null)
        {
            // check if another tag with the name already exists
            if (await _context.ProjectTags.AnyAsync(t => t.name_project_tag == projectTagDto.name_project_tag && t.id_project_tag != id))
            {
                throw new InvalidOperationException($"ProjectTag with name '{projectTagDto.name_project_tag}' already exists");
            }
            projectTagToUpdate.name_project_tag = projectTagDto.name_project_tag;
        }
        if (projectTagDto.weight_project_tag is not null)
        {
            projectTagToUpdate.weight_project_tag = projectTagDto.weight_project_tag.Value;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjectTagDto>(projectTagToUpdate);
    }

    public async Task DeleteProjectTag(int id)
    {
        var projectTagToDelete = await _context.ProjectTags.FindAsync(id) ?? throw new KeyNotFoundException($"ProjectTag with id '{id}' not found");
        _context.ProjectTags.Remove(projectTagToDelete);
        await _context.SaveChangesAsync();
    }
}