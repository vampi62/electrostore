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

    public async Task<PaginatedResponseDto<ReadExtendedProjectCommentDto>> GetProjetCommentairesByProjetId(int projetId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the project exists
        if (!await _context.Projects.AnyAsync(p => p.id_project == projetId))
        {
            throw new KeyNotFoundException($"Project with id '{projetId}' not found");
        }
        var query = _context.ProjectsComments.AsQueryable();
        var filterResult = default(Expression<Func<ProjectsComments, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_project", SearchType = "eq", Value = projetId.ToString() });
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
        var projetCommentaire = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjectCommentDto>
        {
            data = _mapper.Map<List<ReadExtendedProjectCommentDto>>(projetCommentaire),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjectsComments.CountAsync(filterResult ?? (pc => pc.id_project == projetId)),
                nextOffset = offset + limit,
                hasMore = await _context.ProjectsComments.Skip(offset + limit).AnyAsync(filterResult ?? (pc => pc.id_project == projetId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<PaginatedResponseDto<ReadExtendedProjectCommentDto>> GetProjetCommentairesByUserId(int userId, int limit = 100, int offset = 0,
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
        var projetCommentaire = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjectCommentDto>
        {
            data = _mapper.Map<List<ReadExtendedProjectCommentDto>>(projetCommentaire),
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

    public async Task<ReadExtendedProjectCommentDto> GetProjetCommentairesById(int id, int? userId = null, int? projetId = null, List<string>? expand = null)
    {
        var query = _context.ProjectsComments.AsQueryable();
        query = query.Where(pc => pc.id_project_comment == id && (projetId == null || pc.id_project == projetId) && (userId == null || pc.id_user == userId));
        if (expand != null && expand.Contains("project"))
        {
            query = query.Include(pc => pc.Project);
        }
        if (expand != null && expand.Contains("user"))
        {
            query = query.Include(pc => pc.User);
        }
        var projetCommentaire = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"ProjectComment with id '{id}' not found");
        return _mapper.Map<ReadExtendedProjectCommentDto>(projetCommentaire);
    }

    public async Task<ReadProjectCommentDto> CreateProjetCommentaire(CreateProjectCommentDto projetCommentaireDto)
    {
        // check if the project exists
        if (!await _context.Projects.AnyAsync(p => p.id_project == projetCommentaireDto.id_project))
        {
            throw new KeyNotFoundException($"Project with id '{projetCommentaireDto.id_project}' not found");
        }
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == projetCommentaireDto.id_user))
        {
            throw new KeyNotFoundException($"User with id '{projetCommentaireDto.id_user}' not found");
        }
        var newProjetCommentaire = _mapper.Map<ProjectsComments>(projetCommentaireDto);
        _context.ProjectsComments.Add(newProjetCommentaire);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjectCommentDto>(newProjetCommentaire);
    }

    public async Task<ReadProjectCommentDto> UpdateProjetCommentaire(int id, UpdateProjectCommentDto projetCommentaireDto, int? userId = null, int? projetId = null)
    {
        var projetCommentaireToUpdate = await _context.ProjectsComments.FindAsync(id);
        if ((projetCommentaireToUpdate is null) || (projetId is not null && projetCommentaireToUpdate.id_project != projetId) || (userId is not null && projetCommentaireToUpdate.id_user != userId))
        {
            throw new KeyNotFoundException($"Commentaire with id '{id}' not found");
        }
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != projetCommentaireToUpdate.id_user && clientRole < UserRole.Moderator)
        {
            throw new UnauthorizedAccessException($"You are not authorized to update this comment");
        }
        projetCommentaireToUpdate.content_project_comment = projetCommentaireDto.content_project_comment ?? projetCommentaireToUpdate.content_project_comment;
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjectCommentDto>(projetCommentaireToUpdate);
    }

    public async Task DeleteProjetCommentaire(int id, int? userId = null, int? projetId = null)
    {
        var projetCommentaireToDelete = await _context.ProjectsComments.FindAsync(id);
        if ((projetCommentaireToDelete is null) || (projetId is not null && projetCommentaireToDelete.id_project != projetId) || (userId is not null && projetCommentaireToDelete.id_user != userId))
        {
            throw new KeyNotFoundException($"ProjectComment with id '{id}' not found");
        }
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != projetCommentaireToDelete.id_user && clientRole < UserRole.Moderator)
        {
            throw new UnauthorizedAccessException($"You are not authorized to delete this comment");
        }
        _context.ProjectsComments.Remove(projetCommentaireToDelete);
        await _context.SaveChangesAsync();
    }
}