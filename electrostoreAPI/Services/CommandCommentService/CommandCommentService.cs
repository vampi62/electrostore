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

    public async Task<PaginatedResponseDto<ReadExtendedCommandCommentDto>> GetCommandsCommentsByCommandId(int CommandId, int limit = 100, int offset = 0,
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
        var commandComment = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedCommandCommentDto>
        {
            data = _mapper.Map<IEnumerable<ReadExtendedCommandCommentDto>>(commandComment),
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

    public async Task<PaginatedResponseDto<ReadExtendedCommandCommentDto>> GetCommandsCommentsByUserId(int userId, int limit = 100, int offset = 0,
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
        var commandComment = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedCommandCommentDto>
        {
            data = _mapper.Map<IEnumerable<ReadExtendedCommandCommentDto>>(commandComment),
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

    public async Task<ReadExtendedCommandCommentDto> GetCommandsCommentById(int id, int? userId = null, int? CommandId = null, List<string>? expand = null)
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
        var commandComment = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Comment with id '{id}' not found");
        return _mapper.Map<ReadExtendedCommandCommentDto>(commandComment);
    }

    public async Task<ReadCommandCommentDto> CreateComment(CreateCommandCommentDto commandCommentDto)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandCommentDto.id_command))
        {
            throw new KeyNotFoundException($"Command with id '{commandCommentDto.id_command}' not found");
        }
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == commandCommentDto.id_user))
        {
            throw new KeyNotFoundException($"User with id '{commandCommentDto.id_user}' not found");
        }
        var newCommandComment = _mapper.Map<CommandsComments>(commandCommentDto);
        _context.CommandsComments.Add(newCommandComment);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCommandCommentDto>(newCommandComment);
    }

    public async Task<ReadCommandCommentDto> UpdateComment(int id, UpdateCommandCommentDto commandCommentDto, int? userId = null, int? CommandId = null)
    {
        var commandCommentToUpdate = await _context.CommandsComments.FindAsync(id);
        if ((commandCommentToUpdate is null) || (CommandId is not null && commandCommentToUpdate.id_command != CommandId) || (userId is not null && commandCommentToUpdate.id_user != userId))
        {
            throw new KeyNotFoundException($"Comment with id '{id}' not found");
        }
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != commandCommentToUpdate.id_user && clientRole < UserRole.Moderator)
        {
            throw new UnauthorizedAccessException($"You are not authorized to update this comment");
        }
        commandCommentToUpdate.content_command_comment = commandCommentDto.content_command_comment ?? commandCommentToUpdate.content_command_comment;
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCommandCommentDto>(commandCommentToUpdate);
    }

    public async Task DeleteComment(int id, int? userId = null, int? CommandId = null)
    {
        var commandCommentToDelete = await _context.CommandsComments.FindAsync(id);
        if ((commandCommentToDelete is null) || (CommandId is not null && commandCommentToDelete.id_command != CommandId) || (userId is not null && commandCommentToDelete.id_user != userId))
        {
            throw new KeyNotFoundException($"Comment with id '{id}' not found");
        }
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != commandCommentToDelete.id_user && clientRole < UserRole.Moderator)
        {
            throw new UnauthorizedAccessException($"You are not authorized to delete this comment");
        }
        _context.CommandsComments.Remove(commandCommentToDelete);
        await _context.SaveChangesAsync();
    }
}