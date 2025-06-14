using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Enums;
using electrostore.Services.SessionService;

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

    public async Task<IEnumerable<ReadExtendedCommandCommentaireDto>> GetCommandsCommentairesByCommandId(int CommandId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == CommandId))
        {
            throw new KeyNotFoundException($"Command with id {CommandId} not found");
        }
        var query = _context.CommandsCommentaires.AsQueryable();
        query = query.Where(cc => cc.id_command == CommandId);
        query = query.OrderByDescending(cc => cc.created_at);
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
        return _mapper.Map<IEnumerable<ReadExtendedCommandCommentaireDto>>(commandCommentaire);
    }

    public async Task<int> GetCommandsCommentairesCountByCommandId(int CommandId)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == CommandId))
        {
            throw new KeyNotFoundException($"Command with id {CommandId} not found");
        }
        return await _context.CommandsCommentaires
            .Where(c => c.id_command == CommandId)
            .CountAsync();
    }

    public async Task<IEnumerable<ReadExtendedCommandCommentaireDto>> GetCommandsCommentairesByUserId(int userId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == userId))
        {
            throw new KeyNotFoundException($"User with id {userId} not found");
        }
        var query = _context.CommandsCommentaires.AsQueryable();
        query = query.Where(cc => cc.id_user == userId);
        query = query.OrderByDescending(cc => cc.created_at);
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
        return _mapper.Map<IEnumerable<ReadExtendedCommandCommentaireDto>>(commandCommentaire);
    }

    public async Task<int> GetCommandsCommentairesCountByUserId(int userId)
    {
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == userId))
        {
            throw new KeyNotFoundException($"User with id {userId} not found");
        }
        return await _context.CommandsCommentaires
            .Where(c => c.id_user == userId)
            .CountAsync();
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
        var commandCommentaire = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Commentaire with id {id} not found");
        return _mapper.Map<ReadExtendedCommandCommentaireDto>(commandCommentaire);
    }

    public async Task<ReadCommandCommentaireDto> CreateCommentaire(CreateCommandCommentaireDto commandCommentaireDto)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandCommentaireDto.id_command))
        {
            throw new KeyNotFoundException($"Command with id {commandCommentaireDto.id_command} not found");
        }
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == commandCommentaireDto.id_user))
        {
            throw new KeyNotFoundException($"User with id {commandCommentaireDto.id_user} not found");
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
            throw new KeyNotFoundException($"Commentaire with id {id} not found");
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
            throw new KeyNotFoundException($"Commentaire with id {id} not found");
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