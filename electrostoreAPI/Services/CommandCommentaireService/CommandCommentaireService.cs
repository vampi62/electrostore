using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.CommandCommentaireService;

public class CommandCommentaireService : ICommandCommentaireService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public CommandCommentaireService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<IEnumerable<ReadExtendedCommandCommentaireDto>> GetCommandsCommentairesByCommandId(int CommandId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(p => p.id_command == CommandId))
        {
            throw new KeyNotFoundException($"Command with id {CommandId} not found");
        }
        var query = _context.CommandsCommentaires.AsQueryable();
        query = query.Where(c => c.id_command == CommandId);
        query = query.OrderByDescending(c => c.created_at);
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("command")) // check if the command is included in the expand list
        {
            query = query.Include(c => c.Command);
        }
        if (expand != null && expand.Contains("user")) // check if the user is included in the expand list
        {
            query = query.Include(c => c.User);
        }
        var commandCommentaire = await query.ToListAsync();
        return _mapper.Map<IEnumerable<ReadExtendedCommandCommentaireDto>>(commandCommentaire);
    }

    public async Task<int> GetCommandsCommentairesCountByCommandId(int CommandId)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(p => p.id_command == CommandId))
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
        query = query.Where(c => c.id_user == userId);
        query = query.OrderByDescending(c => c.created_at);
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("command")) // check if the command is included in the expand list
        {
            query = query.Include(c => c.Command);
        }
        if (expand != null && expand.Contains("user")) // check if the user is included in the expand list
        {
            query = query.Include(c => c.User);
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
        query = query.Where(c => c.id_command_commentaire == id && (CommandId == null || c.id_command == CommandId) && (userId == null || c.id_user == userId));
        if (expand != null && expand.Contains("command")) // check if the command is included in the expand list
        {
            query = query.Include(c => c.Command);
        }
        if (expand != null && expand.Contains("user")) // check if the user is included in the expand list
        {
            query = query.Include(c => c.User);
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
        var newCommentaire = new CommandsCommentaires
        {
            id_command = commandCommentaireDto.id_command,
            id_user = commandCommentaireDto.id_user,
            contenu_command_commentaire = commandCommentaireDto.contenu_command_commentaire
        };
        _context.CommandsCommentaires.Add(newCommentaire);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCommandCommentaireDto>(newCommentaire);
    }

    public async Task<ReadCommandCommentaireDto> UpdateCommentaire(int id, UpdateCommandCommentaireDto commandCommentaireDto, int? userId = null, int? CommandId = null)
    {
        var commentaireToUpdate = await _context.CommandsCommentaires.FindAsync(id);
        if ((commentaireToUpdate is null) || (CommandId is not null && commentaireToUpdate.id_command != CommandId) || (userId is not null && commentaireToUpdate.id_user != userId))
        {
            throw new KeyNotFoundException($"Commentaire with id {id} not found");
        }
        if (commandCommentaireDto.contenu_command_commentaire is not null)
        {
            commentaireToUpdate.contenu_command_commentaire = commandCommentaireDto.contenu_command_commentaire;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCommandCommentaireDto>(commentaireToUpdate);
    }

    public async Task DeleteCommentaire(int id, int? userId = null, int? CommandId = null)
    {
        var commentaireToDelete = await _context.CommandsCommentaires.FindAsync(id);
        if ((commentaireToDelete is null) || (CommandId is not null && commentaireToDelete.id_command != CommandId) || (userId is not null && commentaireToDelete.id_user != userId))
        {
            throw new KeyNotFoundException($"Commentaire with id {id} not found");
        }
        _context.CommandsCommentaires.Remove(commentaireToDelete);
        await _context.SaveChangesAsync();
    }
}