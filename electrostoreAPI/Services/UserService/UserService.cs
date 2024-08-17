using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

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

    public async Task<ActionResult<ReadUserDto>> CreateUser(CreateUserDto userDto)
    {
        // check email format
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(userDto.email_user))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { email_user = new string[] { "Invalid email format" }}});
        }
        // check password length and if it contain a number and a special character and a uppercase letter and a lowercase letter and if it's at least 8 characters long
        if (!new System.ComponentModel.DataAnnotations.RegularExpressionAttribute(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$").IsValid(userDto.mdp_user))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { mdp_user = new string[] { "password must contain a number and a special character and a uppercase letter and a lowercase letter and if it's at least 8 characters long" }}});
        }
        // Check if email is already used
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == userDto.email_user);
        if (user != null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { email_user = new string[] { "Email already used" }}});
        }
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

    public async Task<ActionResult<ReadUserDto>> GetUserById(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_user = new string[] { "User not found" }}});
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

    public async Task<ActionResult<ReadUserDto>> GetUserByEmail(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == email);

        if (user == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { email_user = new string[] { "User not found" }}});
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

    public async Task<ActionResult<ReadUserDto>> UpdateUser(int id, UpdateUserDto userDto)
    {
        var userToUpdate = await _context.Users.FindAsync(id);

        if (userToUpdate == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_user = new string[] { "User not found" } }});
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
            // check email format
            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(userDto.email_user))
            {
                return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { email_user = new string[] { "Invalid email format" } }});
            }
            // Check if email is already used
            var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == userDto.email_user);
            if (user != null)
            {
                return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { email_user = new string[] { "Email already used" } }});
            }
            userToUpdate.email_user = userDto.email_user;
        }

        if (userDto.mdp_user != null)
        {
            // check password length and if it contain a number and a special character and a uppercase letter and a lowercase letter and if it's at least 8 characters long
            if (!new System.ComponentModel.DataAnnotations.RegularExpressionAttribute(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$").IsValid(userDto.mdp_user))
            {
                return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { mdp_user = new string[] { "password must contain a number and a special character and a uppercase letter and a lowercase letter and if it's at least 8 characters long" } }});
            }
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

    public async Task<ActionResult> DeleteUser(int id)
    {
        var userToDelete = await _context.Users.FindAsync(id);

        if (userToDelete == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_user = new string[] { "User not found" }}});
        }

        _context.Users.Remove(userToDelete);
        await _context.SaveChangesAsync();
        return new OkResult();
    }

    public async Task<ActionResult<bool>> CheckUserPassword(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == email);

        if (user == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { email_user = new string[] { "User not found" } }});
        }

        return BCrypt.Net.BCrypt.Verify(password, user.mdp_user);
    }
}