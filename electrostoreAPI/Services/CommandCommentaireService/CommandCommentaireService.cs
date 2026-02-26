using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Enums;
using electrostore.Extensions;
using electrostore.Models;
using electrostore.Services.SessionService;
using System.Linq.Expressions;

namespace electrostore.Services.CommandCommentaireService;

public class CommandCommentaireService : ICommandCommentaireService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;

    public CommandCommentaireService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
    }

    public async Task<PaginatedResponseDto<ReadExtendedCommandCommentaireDto>> GetCommandsCommentairesByCommandId(int CommandId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == CommandId))
        {
            throw new KeyNotFoundException($"Command with id '{CommandId}' not found");
        }
        var query = _context.CommandsCommentaires.AsQueryable();
        var filterResult = default(Expression<Func<CommandsCommentaires, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_command", SearchType = "eq", Value = CommandId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<CommandsCommentaires>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<CommandsCommentaires>(sort);
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
        return new PaginatedResponseDto<ReadExtendedCommandCommentaireDto>
        {
            data = _mapper.Map<IEnumerable<ReadExtendedCommandCommentaireDto>>(commandCommentaire),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.CommandsCommentaires.CountAsync(filterResult ?? (cc => cc.id_command == CommandId)),
                nextOffset = offset + limit,
                hasMore = await _context.CommandsCommentaires.Skip(offset + limit).AnyAsync(filterResult ?? (cc => cc.id_command == CommandId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<PaginatedResponseDto<ReadExtendedCommandCommentaireDto>> GetCommandsCommentairesByUserId(int userId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == userId))
        {
            throw new KeyNotFoundException($"User with id '{userId}' not found");
        }
        var query = _context.CommandsCommentaires.AsQueryable();
        var filterResult = default(Expression<Func<CommandsCommentaires, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_user", SearchType = "eq", Value = userId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<CommandsCommentaires>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<CommandsCommentaires>(sort);
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
        return new PaginatedResponseDto<ReadExtendedCommandCommentaireDto>
        {
            data = _mapper.Map<IEnumerable<ReadExtendedCommandCommentaireDto>>(commandCommentaire),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.CommandsCommentaires.CountAsync(filterResult ?? (cc => cc.id_user == userId)),
                nextOffset = offset + limit,
                hasMore = await _context.CommandsCommentaires.Skip(offset + limit).AnyAsync(filterResult ?? (cc => cc.id_user == userId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedCommandCommentaireDto> GetCommandsCommentaireById(int id, int? userId = null, int? CommandId = null, List<string>? expand = null)
    {
        var query = _context.CommandsCommentaires.AsQueryable();
        query = query.Where(cc => cc.id_command_commentaire == id && (CommandId == null || cc.id_command == CommandId) && (userId == null || cc.id_user == userId));
        if (expand != null && expand.Contains("command")) // check if the command is included in the expand list
        {
            query = query.Include(cc => cc.Command);
        }
        if (expand != null && expand.Contains("user")) // check if the user is included in the expand list
        {
            query = query.Include(cc => cc.User);
        }
        var commandCommentaire = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Commentaire with id '{id}' not found");
        return _mapper.Map<ReadExtendedCommandCommentaireDto>(commandCommentaire);
    }

    public async Task<ReadCommandCommentaireDto> CreateCommentaire(CreateCommandCommentaireDto commandCommentaireDto)
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
        var newCommandCommentaire = _mapper.Map<CommandsCommentaires>(commandCommentaireDto);
        _context.CommandsCommentaires.Add(newCommandCommentaire);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCommandCommentaireDto>(newCommandCommentaire);
    }

    public async Task<ReadCommandCommentaireDto> UpdateCommentaire(int id, UpdateCommandCommentaireDto commandCommentaireDto, int? userId = null, int? CommandId = null)
    {
        var commandCommentaireToUpdate = await _context.CommandsCommentaires.FindAsync(id);
        if ((commandCommentaireToUpdate is null) || (CommandId is not null && commandCommentaireToUpdate.id_command != CommandId) || (userId is not null && commandCommentaireToUpdate.id_user != userId))
        {
            throw new KeyNotFoundException($"Commentaire with id '{id}' not found");
        }
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != commandCommentaireToUpdate.id_user && clientRole < UserRole.Moderator)
        {
            throw new UnauthorizedAccessException($"You are not authorized to update this commentaire");
        }
        commandCommentaireToUpdate.contenu_command_commentaire = commandCommentaireDto.contenu_command_commentaire ?? commandCommentaireToUpdate.contenu_command_commentaire;
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCommandCommentaireDto>(commandCommentaireToUpdate);
    }

    public async Task DeleteCommentaire(int id, int? userId = null, int? CommandId = null)
    {
        var commandCommentaireToDelete = await _context.CommandsCommentaires.FindAsync(id);
        if ((commandCommentaireToDelete is null) || (CommandId is not null && commandCommentaireToDelete.id_command != CommandId) || (userId is not null && commandCommentaireToDelete.id_user != userId))
        {
            throw new KeyNotFoundException($"Commentaire with id '{id}' not found");
        }
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != commandCommentaireToDelete.id_user && clientRole < UserRole.Moderator)
        {
            throw new UnauthorizedAccessException($"You are not authorized to delete this commentaire");
        }
        _context.CommandsCommentaires.Remove(commandCommentaireToDelete);
        await _context.SaveChangesAsync();
    }
}