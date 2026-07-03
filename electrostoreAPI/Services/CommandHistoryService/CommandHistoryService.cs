using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.CommandHistoryService;

public class CommandHistoryService : ICommandHistoryService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public CommandHistoryService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<PaginatedResponseDto<ReadCommandHistoryDto>> GetCommandHistoryByCommandId(int idCommand, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == idCommand))
        {
            throw new KeyNotFoundException($"Command with id '{idCommand}' not found");
        }
        var query = _context.CommandsHistory.AsQueryable();
        var filterResult = default(Expression<Func<CommandsHistory, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_command", SearchType = "eq", Value = idCommand.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<CommandsHistory>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<CommandsHistory>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "created_at", Order = "desc" };
                query = query.OrderByDescending(ch => ch.created_at);
            }
        }
        else
        {
            sort = new SorterDto { Field = "created_at", Order = "desc" };
            query = query.OrderByDescending(ch => ch.created_at);
        }
        query = query.Skip(offset).Take(limit);
        var commandHistory = await query.ToListAsync();
        return new PaginatedResponseDto<ReadCommandHistoryDto>
        {
            data = _mapper.Map<List<ReadCommandHistoryDto>>(commandHistory),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.CommandsHistory.CountAsync(filterResult ?? (ci => ci.id_command == idCommand)),
                nextOffset = offset + limit,
                hasMore = await _context.CommandsHistory.Skip(offset + limit).AnyAsync(filterResult ?? (ci => ci.id_command == idCommand))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadCommandHistoryDto> GetCommandHistoryById(int id, int idCommand)
    {
        var query = _context.CommandsHistory.AsQueryable();
        query = query.Where(pth => pth.id_command_history == id && pth.id_command == idCommand);
        var commandHistory = await query.FirstOrDefaultAsync()?? throw new KeyNotFoundException($"CommandHistory with id '{id}' not found for Command with id '{idCommand}'");
        return _mapper.Map<ReadCommandHistoryDto>(commandHistory);
    }

    public async Task<ReadCommandHistoryDto> CreateCommandHistory(CreateCommandHistoryDto commandHistoryDto)
    {
        var command = await _context.Commands.FirstOrDefaultAsync(c => c.id_command == commandHistoryDto.id_command) ?? throw new KeyNotFoundException($"Command with id '{commandHistoryDto.id_command}' not found");
        var newCommandHistory = _mapper.Map<CommandsHistory>(commandHistoryDto);
        _context.CommandsHistory.Add(newCommandHistory);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCommandHistoryDto>(newCommandHistory);
    }
}