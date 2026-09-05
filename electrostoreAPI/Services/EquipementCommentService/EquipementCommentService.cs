using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.SessionService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.EquipementCommentService;

public class EquipementCommentService : IEquipementCommentService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;

    public EquipementCommentService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
    }

    public async Task<PaginatedResponseDto<ReadExtendedEquipementCommentDto>> GetEquipementsCommentsByEquipementId(int equipementId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the equipement exists
        if (!await _context.Equipements.AnyAsync(e => e.id_equipement == equipementId))
        {
            throw new KeyNotFoundException($"Equipement with id '{equipementId}' not found");
        }
        var query = _context.EquipementsComments.AsQueryable();
        var filterResult = default(Expression<Func<EquipementsComments, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { field = "id_equipement", search_type = "eq", value = equipementId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<EquipementsComments>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<EquipementsComments>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { field = "created_at", order = "desc" };
                query = query.OrderByDescending(ec => ec.created_at);
            }
        }
        else
        {
            query = query.OrderByDescending(ec => ec.created_at);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("equipement"))
        {
            query = query.Include(ec => ec.Equipement);
        }
        if (expand != null && expand.Contains("user"))
        {
            query = query.Include(ec => ec.User);
        }
        var equipementComment = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedEquipementCommentDto>
        {
            data = _mapper.Map<IEnumerable<ReadExtendedEquipementCommentDto>>(equipementComment),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.EquipementsComments.CountAsync(filterResult ?? (ec => ec.id_equipement == equipementId)),
                next_offset = offset + limit,
                has_more = await _context.EquipementsComments.Skip(offset + limit).AnyAsync(filterResult ?? (ec => ec.id_equipement == equipementId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<PaginatedResponseDto<ReadExtendedEquipementCommentDto>> GetEquipementsCommentsByUserId(int userId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == userId))
        {
            throw new KeyNotFoundException($"User with id '{userId}' not found");
        }
        var query = _context.EquipementsComments.AsQueryable();
        var filterResult = default(Expression<Func<EquipementsComments, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { field = "id_user", search_type = "eq", value = userId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<EquipementsComments>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<EquipementsComments>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { field = "created_at", order = "desc" };
                query = query.OrderByDescending(ec => ec.created_at);
            }
        }
        else
        {
            query = query.OrderByDescending(ec => ec.created_at);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("equipement"))
        {
            query = query.Include(ec => ec.Equipement);
        }
        if (expand != null && expand.Contains("user"))
        {
            query = query.Include(ec => ec.User);
        }
        var equipementComment = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedEquipementCommentDto>
        {
            data = _mapper.Map<IEnumerable<ReadExtendedEquipementCommentDto>>(equipementComment),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.EquipementsComments.CountAsync(filterResult ?? (ec => ec.id_user == userId)),
                next_offset = offset + limit,
                has_more = await _context.EquipementsComments.Skip(offset + limit).AnyAsync(filterResult ?? (ec => ec.id_user == userId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedEquipementCommentDto> GetEquipementCommentById(int id, int? userId = null, int? equipementId = null, List<string>? expand = null)
    {
        var query = _context.EquipementsComments.AsQueryable();
        query = query.Where(ec => ec.id_equipement_comment == id && (equipementId == null || ec.id_equipement == equipementId) && (userId == null || ec.id_user == userId));
        if (expand != null && expand.Contains("equipement"))
        {
            query = query.Include(ec => ec.Equipement);
        }
        if (expand != null && expand.Contains("user"))
        {
            query = query.Include(ec => ec.User);
        }
        var equipementComment = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Comment with id '{id}' not found");
        return _mapper.Map<ReadExtendedEquipementCommentDto>(equipementComment);
    }

    public async Task<ReadEquipementCommentDto> CreateComment(CreateEquipementCommentDto equipementCommentDto)
    {
        // check if the equipement exists
        if (!await _context.Equipements.AnyAsync(e => e.id_equipement == equipementCommentDto.id_equipement))
        {
            throw new KeyNotFoundException($"Equipement with id '{equipementCommentDto.id_equipement}' not found");
        }
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == equipementCommentDto.id_user))
        {
            throw new KeyNotFoundException($"User with id '{equipementCommentDto.id_user}' not found");
        }
        var newEquipementComment = _mapper.Map<EquipementsComments>(equipementCommentDto);
        _context.EquipementsComments.Add(newEquipementComment);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadEquipementCommentDto>(newEquipementComment);
    }

    public async Task<ReadEquipementCommentDto> UpdateComment(int id, UpdateEquipementCommentDto equipementCommentDto, int? userId = null, int? equipementId = null)
    {
        var equipementCommentToUpdate = await _context.EquipementsComments.FindAsync(id);
        if ((equipementCommentToUpdate is null) || (equipementId is not null && equipementCommentToUpdate.id_equipement != equipementId) || (userId is not null && equipementCommentToUpdate.id_user != userId))
        {
            throw new KeyNotFoundException($"Comment with id '{id}' not found");
        }
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != equipementCommentToUpdate.id_user && clientRole < UserRole.Moderator)
        {
            throw new UnauthorizedAccessException($"You are not authorized to update this comment");
        }
        equipementCommentToUpdate.content_equipement_comment = equipementCommentDto.content_equipement_comment ?? equipementCommentToUpdate.content_equipement_comment;
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadEquipementCommentDto>(equipementCommentToUpdate);
    }

    public async Task DeleteComment(int id, int? userId = null, int? equipementId = null)
    {
        var equipementCommentToDelete = await _context.EquipementsComments.FindAsync(id);
        if ((equipementCommentToDelete is null) || (equipementId is not null && equipementCommentToDelete.id_equipement != equipementId) || (userId is not null && equipementCommentToDelete.id_user != userId))
        {
            throw new KeyNotFoundException($"Comment with id '{id}' not found");
        }
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != equipementCommentToDelete.id_user && clientRole < UserRole.Moderator)
        {
            throw new UnauthorizedAccessException($"You are not authorized to delete this comment");
        }
        _context.EquipementsComments.Remove(equipementCommentToDelete);
        await _context.SaveChangesAsync();
    }
}
