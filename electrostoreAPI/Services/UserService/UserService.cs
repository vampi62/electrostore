using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.UserService;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadUserDto>> GetUsers(int limit = 100, int offset = 0)
    {
        return await _context.Users
            .Skip(offset)
            .Take(limit)
            .Select(u => new ReadUserDto
            {
                id_user = u.id_user,
                nom_user = u.nom_user,
                prenom_user = u.prenom_user,
                email_user = u.email_user,
                role_user = u.role_user
            })
            .ToListAsync();
    }

    public async Task<ReadUserDto> CreateUser(CreateUserDto userDto)
    {
        var newUser = new Users
        {
            nom_user = userDto.nom_user,
            prenom_user = userDto.prenom_user,
            email_user = userDto.email_user,
            mdp_user = BCrypt.Net.BCrypt.HashPassword(userDto.mdp_user),
            role_user = userDto.role_user
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        return new ReadUserDto
        {
            id_user = newUser.id_user,
            nom_user = newUser.nom_user,
            prenom_user = newUser.prenom_user,
            email_user = newUser.email_user,
            role_user = newUser.role_user
        };
    }

    public async Task<ReadUserDto> GetUserById(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            throw new ArgumentException("User not found");
        }

        return new ReadUserDto
        {
            id_user = user.id_user,
            nom_user = user.nom_user,
            prenom_user = user.prenom_user,
            email_user = user.email_user,
            role_user = user.role_user
        };
    }

    public async Task<ReadUserDto> GetUserByEmail(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == email);

        if (user == null)
        {
            throw new ArgumentException("User not found");
        }

        return new ReadUserDto
        {
            id_user = user.id_user,
            nom_user = user.nom_user,
            prenom_user = user.prenom_user,
            email_user = user.email_user,
            role_user = user.role_user
        };
    }

    public async Task<ReadUserDto> UpdateUser(int id, UpdateUserDto userDto)
    {
        var userToUpdate = await _context.Users.FindAsync(id);

        if (userToUpdate == null)
        {
            throw new ArgumentException("User not found");
        }

        if (userDto.nom_user != null)
        {
            userToUpdate.nom_user = userDto.nom_user;
        }

        if (userDto.prenom_user != null)
        {
            userToUpdate.prenom_user = userDto.prenom_user;
        }

        if (userDto.email_user != null)
        {
            userToUpdate.email_user = userDto.email_user;
        }

        if (userDto.mdp_user != null)
        {
            userToUpdate.mdp_user = BCrypt.Net.BCrypt.HashPassword(userDto.mdp_user);
        }

        if (userDto.role_user != null)
        {
            userToUpdate.role_user = userDto.role_user;
        }

        await _context.SaveChangesAsync();

        return new ReadUserDto
        {
            id_user = userToUpdate.id_user,
            nom_user = userToUpdate.nom_user,
            prenom_user = userToUpdate.prenom_user,
            email_user = userToUpdate.email_user,
            role_user = userToUpdate.role_user
        };
    }

    public async Task DeleteUser(int id)
    {
        var userToDelete = await _context.Users.FindAsync(id);

        if (userToDelete == null)
        {
            throw new ArgumentException("User not found");
        }

        _context.Users.Remove(userToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> CheckUserPassword(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == email);

        if (user == null)
        {
            throw new ArgumentException("User not found");
        }

        return BCrypt.Net.BCrypt.Verify(password, user.mdp_user);
    }
}