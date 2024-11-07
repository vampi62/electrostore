using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.CommandService;

public class CommandService : ICommandService
{
    private readonly ApplicationDbContext _context;

    public CommandService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadCommandDto>> GetCommands(int limit = 100, int offset = 0)
    {
        return await _context.Commands
            .Select(c => new ReadCommandDto
            {
                id_command = c.id_command,
                prix_command = c.prix_command,
                url_command = c.url_command,
                status_command = c.status_command,
                date_command = c.date_command,
                date_livraison_command = c.date_livraison_command
            })
            .ToListAsync();
    }

    public async Task<ReadCommandDto> GetCommandById(int id)
    {
        var command = await _context.Commands.FindAsync(id) ?? throw new KeyNotFoundException($"Command with id {id} not found");
        return new ReadCommandDto
        {
            id_command = command.id_command,
            prix_command = command.prix_command,
            url_command = command.url_command,
            status_command = command.status_command,
            date_command = command.date_command,
            date_livraison_command = command.date_livraison_command
        };
    }

    public async Task<ReadCommandDto> CreateCommand(CreateCommandDto commandDto)
    {
        var newCommand = new Commands
        {
            prix_command = commandDto.prix_command,
            url_command = commandDto.url_command,
            status_command = commandDto.status_command,
            date_command = commandDto.date_command,
            date_livraison_command = commandDto.date_livraison_command
        };
        _context.Commands.Add(newCommand);
        await _context.SaveChangesAsync();
        if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commandDocuments", newCommand.id_command.ToString())))
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commandDocuments", newCommand.id_command.ToString()));
        }
        return new ReadCommandDto
        {
            id_command = newCommand.id_command,
            prix_command = newCommand.prix_command,
            url_command = newCommand.url_command,
            status_command = newCommand.status_command,
            date_command = newCommand.date_command,
            date_livraison_command = newCommand.date_livraison_command
        };
    }

    public async Task<ReadCommandDto> UpdateCommand(int id, UpdateCommandDto commandDto)
    {
        var commandToUpdate = await _context.Commands.FindAsync(id) ?? throw new KeyNotFoundException($"Command with id {id} not found");
        if (commandDto.prix_command != null)
        {
            commandToUpdate.prix_command = commandDto.prix_command.Value;
        }
        if (commandDto.url_command != null)
        {
            commandToUpdate.url_command = commandDto.url_command;
        }
        if (commandDto.status_command != null)
        {
            commandToUpdate.status_command = commandDto.status_command;
        }
        if (commandDto.date_command != null)
        {
            commandToUpdate.date_command = commandDto.date_command.Value;
        }
        if (commandDto.date_livraison_command != null)
        {
            commandToUpdate.date_livraison_command = commandDto.date_livraison_command;
        }
        await _context.SaveChangesAsync();
        return new ReadCommandDto
        {
            id_command = commandToUpdate.id_command,
            prix_command = commandToUpdate.prix_command,
            url_command = commandToUpdate.url_command,
            status_command = commandToUpdate.status_command,
            date_command = commandToUpdate.date_command,
            date_livraison_command = commandToUpdate.date_livraison_command
        };
    }

    public async Task DeleteCommand(int id)
    {
        var commandToDelete = await _context.Commands.FindAsync(id) ?? throw new KeyNotFoundException($"Command with id {id} not found");
        _context.Commands.Remove(commandToDelete);
        await _context.SaveChangesAsync();
        //remove folder in wwwroot/commandDocuments
        if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commandDocuments", id.ToString())))
        {
            Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commandDocuments", id.ToString()), true);
        }
    }
}
