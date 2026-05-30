using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectrostoreAPI.Services.CommandHistoryService;

public class CommandHistoryService : ICommandHistoryService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public CommandHistoryService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper  = mapper;
        _context = context;
    }

    public async Task<PaginatedResponseDto<ReadCommandHistoryDto>> GetCommandHistoriesByCommandId(
        int idCommand, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(p => p.id_command == idCommand))
        {
            throw new KeyNotFoundException($"Command with id '{idCommand}' not found");
        }
        var query = _context.CommandsHistory.AsQueryable();
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_command", SearchType = "eq", Value = idCommand.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            var filterResult = RsqlParserExtensions.ToFilterExpression<CommandsHistory>(rsql);
            query = query.Where(filterResult.Item1);
            rsql = filterResult.Item2;
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
                query = query.OrderByDescending(p => p.created_at);
            }
        }
        else
        {
            query = query.OrderByDescending(p => p.created_at);
        }
        query = query.Skip(offset).Take(limit);
        var commandHistories = await query.ToListAsync();
        return new PaginatedResponseDto<ReadCommandHistoryDto>
        {
            data = _mapper.Map<List<ReadCommandHistoryDto>>(commandHistories),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.CommandsHistory.CountAsync(p => p.id_command == idCommand),
                nextOffset = offset + limit,
                hasMore = await _context.CommandsHistory.Skip(offset + limit).AnyAsync(p => p.id_command == idCommand)
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadCommandHistoryDto> GetCommandHistoryById(int id, int? idCommand = null)
    {
        var query = _context.CommandsHistory.AsQueryable();
        query = query.Where(pc => pc.id_command_history == id && (idCommand == null || pc.id_command == idCommand));
        var commandHistory = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Command history with id '{id}' not found");
        return _mapper.Map<ReadCommandHistoryDto>(commandHistory);
    }
}
