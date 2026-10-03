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
        rsql ??= [];
        rsql.Add(new FilterDto { field = "id_command", search_type = "eq", value = idCommand.ToString() });
        return await query
            .ToPagedQuery(limit, offset, rsql, sort, new SorterDto { field = "created_at", order = "desc" })
            .ToResponseAsync(commandHistory => _mapper.Map<List<ReadCommandHistoryDto>>(commandHistory));
    }

    public async Task<ReadCommandHistoryDto> GetCommandHistoryById(int id, int idCommand)
    {
        var query = _context.CommandsHistory.AsQueryable();
        query = query.Where(pth => pth.id_command_history == id && pth.id_command == idCommand);
        var commandHistory = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"CommandHistory with id '{id}' not found for Command with id '{idCommand}'");
        return _mapper.Map<ReadCommandHistoryDto>(commandHistory);
    }

    public async Task<ReadCommandHistoryDto> CreateCommandHistory(CreateCommandHistoryDto commandHistoryDto)
    {
        _ = await _context.Commands.FindAsync(commandHistoryDto.id_command) ?? throw new KeyNotFoundException($"Command with id '{commandHistoryDto.id_command}' not found");
        var newCommandHistory = _mapper.Map<CommandsHistory>(commandHistoryDto);
        _context.CommandsHistory.Add(newCommandHistory);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCommandHistoryDto>(newCommandHistory);
    }
}