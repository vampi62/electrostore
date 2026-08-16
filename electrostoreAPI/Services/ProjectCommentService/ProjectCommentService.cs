using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.SessionService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.ProjectCommentService;

public class ProjectCommentService : IProjectCommentService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;

    public ProjectCommentService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
    }

    public async Task<PaginatedResponseDto<ReadExtendedProjectCommentDto>> GetProjectCommentsByProjectId(int projectId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the project exists
        if (!await _context.Projects.AnyAsync(p => p.id_project == projectId))
        {
            throw new KeyNotFoundException($"Project with id '{projectId}' not found");
        }
        var query = _context.ProjectsComments.AsQueryable();
        var filterResult = default(Expression<Func<ProjectsComments, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_project", SearchType = "eq", Value = projectId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ProjectsComments>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ProjectsComments>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "created_at", Order = "desc" };
                query = query.OrderByDescending(p => p.created_at);
            }
        }
        else
        {
            query = query.OrderByDescending(p => p.created_at);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("project"))
        {
            query = query.Include(p => p.Project);
        }
        if (expand != null && expand.Contains("user"))
        {
            query = query.Include(p => p.User);
        }
        var projectComment = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjectCommentDto>
        {
            data = _mapper.Map<List<ReadExtendedProjectCommentDto>>(projectComment),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjectsComments.CountAsync(filterResult ?? (pc => pc.id_project == projectId)),
                nextOffset = offset + limit,
                hasMore = await _context.ProjectsComments.Skip(offset + limit).AnyAsync(filterResult ?? (pc => pc.id_project == projectId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<PaginatedResponseDto<ReadExtendedProjectCommentDto>> GetProjectCommentsByUserId(int userId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == userId))
        {
            throw new KeyNotFoundException($"User with id '{userId}' not found");
        }
        var query = _context.ProjectsComments.AsQueryable();
        var filterResult = default(Expression<Func<ProjectsComments, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_user", SearchType = "eq", Value = userId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ProjectsComments>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ProjectsComments>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "created_at", Order = "desc" };
                query = query.OrderByDescending(p => p.created_at);
            }
        }
        else
        {
            query = query.OrderByDescending(p => p.created_at);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("project"))
        {
            query = query.Include(pc => pc.Project);
        }
        if (expand != null && expand.Contains("user"))
        {
            query = query.Include(pc => pc.User);
        }
        var projectComment = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjectCommentDto>
        {
            data = _mapper.Map<List<ReadExtendedProjectCommentDto>>(projectComment),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjectsComments.CountAsync(filterResult ?? (pc => pc.id_user == userId)),
                nextOffset = offset + limit,
                hasMore = await _context.ProjectsComments.Skip(offset + limit).AnyAsync(filterResult ?? (pc => pc.id_user == userId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedProjectCommentDto> GetProjectCommentsById(int id, int? userId = null, int? projectId = null, List<string>? expand = null)
    {
        var query = _context.ProjectsComments.AsQueryable();
        query = query.Where(pc => pc.id_project_comment == id && (projectId == null || pc.id_project == projectId) && (userId == null || pc.id_user == userId));
        if (expand != null && expand.Contains("project"))
        {
            query = query.Include(pc => pc.Project);
        }
        if (expand != null && expand.Contains("user"))
        {
            query = query.Include(pc => pc.User);
        }
        var projectComment = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"ProjectComment with id '{id}' not found");
        return _mapper.Map<ReadExtendedProjectCommentDto>(projectComment);
    }

    public async Task<ReadProjectCommentDto> CreateProjectComment(CreateProjectCommentDto projectCommentDto)
    {
        // check if the project exists
        if (!await _context.Projects.AnyAsync(p => p.id_project == projectCommentDto.id_project))
        {
            throw new KeyNotFoundException($"Project with id '{projectCommentDto.id_project}' not found");
        }
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == projectCommentDto.id_user))
        {
            throw new KeyNotFoundException($"User with id '{projectCommentDto.id_user}' not found");
        }
        var newProjectComment = _mapper.Map<ProjectsComments>(projectCommentDto);
        _context.ProjectsComments.Add(newProjectComment);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjectCommentDto>(newProjectComment);
    }

    public async Task<ReadProjectCommentDto> UpdateProjectComment(int id, UpdateProjectCommentDto projectCommentDto, int? userId = null, int? projectId = null)
    {
        var projectCommentToUpdate = await _context.ProjectsComments.FindAsync(id);
        if ((projectCommentToUpdate is null) || (projectId is not null && projectCommentToUpdate.id_project != projectId) || (userId is not null && projectCommentToUpdate.id_user != userId))
        {
            throw new KeyNotFoundException($"Comment with id '{id}' not found");
        }
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != projectCommentToUpdate.id_user && clientRole < UserRole.Moderator)
        {
            throw new UnauthorizedAccessException($"You are not authorized to update this comment");
        }
        projectCommentToUpdate.content_project_comment = projectCommentDto.content_project_comment ?? projectCommentToUpdate.content_project_comment;
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjectCommentDto>(projectCommentToUpdate);
    }

    public async Task DeleteProjectComment(int id, int? userId = null, int? projectId = null)
    {
        var projectCommentToDelete = await _context.ProjectsComments.FindAsync(id);
        if ((projectCommentToDelete is null) || (projectId is not null && projectCommentToDelete.id_project != projectId) || (userId is not null && projectCommentToDelete.id_user != userId))
        {
            throw new KeyNotFoundException($"ProjectComment with id '{id}' not found");
        }
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != projectCommentToDelete.id_user && clientRole < UserRole.Moderator)
        {
            throw new UnauthorizedAccessException($"You are not authorized to delete this comment");
        }
        _context.ProjectsComments.Remove(projectCommentToDelete);
        await _context.SaveChangesAsync();
    }
}