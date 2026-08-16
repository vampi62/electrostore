using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.SessionService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.CommandCommentService;

public class CommandCommentService : ICommandCommentService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;

    public CommandCommentService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
    }

    public async Task<PaginatedResponseDto<ReadExtendedCommandCommentDto>> GetCommandsCommentairesByCommandId(int CommandId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == CommandId))
        {
            throw new KeyNotFoundException($"Command with id '{CommandId}' not found");
        }
        var query = _context.CommandsComments.AsQueryable();
        var filterResult = default(Expression<Func<CommandsComments, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_command", SearchType = "eq", Value = CommandId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<CommandsComments>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<CommandsComments>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "created_at", Order = "desc" };
                query = query.OrderByDescending(cc => cc.created_at);
            }
        }
        else
        {
            query = query.OrderByDescending(cc => cc.created_at);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("command")) // check if the command is included in the expand list
        {
            query = query.Include(cc => cc.Command);
        }
        if (expand != null && expand.Contains("user")) // check if the user is included in the expand list
        {
            query = query.Include(cc => cc.User);
        }
        var commandCommentaire = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedCommandCommentDto>
        {
            data = _mapper.Map<IEnumerable<ReadExtendedCommandCommentDto>>(commandCommentaire),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.CommandsComments.CountAsync(filterResult ?? (cc => cc.id_command == CommandId)),
                nextOffset = offset + limit,
                hasMore = await _context.CommandsComments.Skip(offset + limit).AnyAsync(filterResult ?? (cc => cc.id_command == CommandId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<PaginatedResponseDto<ReadExtendedCommandCommentDto>> GetCommandsCommentairesByUserId(int userId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == userId))
        {
            throw new KeyNotFoundException($"User with id '{userId}' not found");
        }
        var query = _context.CommandsComments.AsQueryable();
        var filterResult = default(Expression<Func<CommandsComments, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_user", SearchType = "eq", Value = userId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<CommandsComments>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<CommandsComments>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "created_at", Order = "desc" };
                query = query.OrderByDescending(cc => cc.created_at);
            }
        }
        else
        {
            query = query.OrderByDescending(cc => cc.created_at);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("command")) // check if the command is included in the expand list
        {
            query = query.Include(cc => cc.Command);
        }
        if (expand != null && expand.Contains("user")) // check if the user is included in the expand list
        {
            query = query.Include(cc => cc.User);
        }
        var commandCommentaire = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedCommandCommentDto>
        {
            data = _mapper.Map<IEnumerable<ReadExtendedCommandCommentDto>>(commandCommentaire),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.CommandsComments.CountAsync(filterResult ?? (cc => cc.id_user == userId)),
                nextOffset = offset + limit,
                hasMore = await _context.CommandsComments.Skip(offset + limit).AnyAsync(filterResult ?? (cc => cc.id_user == userId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedCommandCommentDto> GetCommandsCommentaireById(int id, int? userId = null, int? CommandId = null, List<string>? expand = null)
    {
        var query = _context.CommandsComments.AsQueryable();
        query = query.Where(cc => cc.id_command_comment == id && (CommandId == null || cc.id_command == CommandId) && (userId == null || cc.id_user == userId));
        if (expand != null && expand.Contains("command")) // check if the command is included in the expand list
        {
            query = query.Include(cc => cc.Command);
        }
        if (expand != null && expand.Contains("user")) // check if the user is included in the expand list
        {
            query = query.Include(cc => cc.User);
        }
        var commandCommentaire = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Commentaire with id '{id}' not found");
        return _mapper.Map<ReadExtendedCommandCommentDto>(commandCommentaire);
    }

    public async Task<ReadCommandCommentDto> CreateCommentaire(CreateCommandCommentDto commandCommentaireDto)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandCommentaireDto.id_command))
        {
            throw new KeyNotFoundException($"Command with id '{commandCommentaireDto.id_command}' not found");
        }
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == commandCommentaireDto.id_user))
        {
            throw new KeyNotFoundException($"User with id '{commandCommentaireDto.id_user}' not found");
        }
        var newCommandCommentaire = _mapper.Map<CommandsComments>(commandCommentaireDto);
        _context.CommandsComments.Add(newCommandCommentaire);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCommandCommentDto>(newCommandCommentaire);
    }

    public async Task<ReadCommandCommentDto> UpdateCommentaire(int id, UpdateCommandCommentDto commandCommentaireDto, int? userId = null, int? CommandId = null)
    {
        var commandCommentaireToUpdate = await _context.CommandsComments.FindAsync(id);
        if ((commandCommentaireToUpdate is null) || (CommandId is not null && commandCommentaireToUpdate.id_command != CommandId) || (userId is not null && commandCommentaireToUpdate.id_user != userId))
        {
            throw new KeyNotFoundException($"Commentaire with id '{id}' not found");
        }
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != commandCommentaireToUpdate.id_user && clientRole < UserRole.Moderator)
        {
            throw new UnauthorizedAccessException($"You are not authorized to update this comment");
        }
        commandCommentaireToUpdate.content_command_comment = commandCommentaireDto.content_command_comment ?? commandCommentaireToUpdate.content_command_comment;
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCommandCommentDto>(commandCommentaireToUpdate);
    }

    public async Task DeleteCommentaire(int id, int? userId = null, int? CommandId = null)
    {
        var commandCommentaireToDelete = await _context.CommandsComments.FindAsync(id);
        if ((commandCommentaireToDelete is null) || (CommandId is not null && commandCommentaireToDelete.id_command != CommandId) || (userId is not null && commandCommentaireToDelete.id_user != userId))
        {
            throw new KeyNotFoundException($"Commentaire with id '{id}' not found");
        }
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != commandCommentaireToDelete.id_user && clientRole < UserRole.Moderator)
        {
            throw new UnauthorizedAccessException($"You are not authorized to delete this comment");
        }
        _context.CommandsComments.Remove(commandCommentaireToDelete);
        await _context.SaveChangesAsync();
    }
}