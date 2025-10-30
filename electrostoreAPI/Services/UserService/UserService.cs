using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Enums;
using electrostore.Services.SmtpService;
using electrostore.Services.SessionService;
using electrostore.Services.JwiService;

namespace electrostore.Services.UserService;

public class UserService : IUserService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISmtpService _smtpService;
    private readonly ISessionService _sessionService;
    private readonly IJwiService _jwiService;
    public UserService(IMapper mapper, ApplicationDbContext context, ISmtpService smtpService, ISessionService sessionService, IJwiService jwiService)
    {
        _mapper = mapper;
        _context = context;
        _smtpService = smtpService;
        _sessionService = sessionService;
        _jwiService = jwiService;
    }

    public async Task<IEnumerable<ReadExtendedUserDto>> GetUsers(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Users.AsQueryable();
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(u => idResearch.Contains(u.id_user));
        }
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(u => u.id_user);
        var user = await query
            .Select(u => new
            {
                User = u,
                ProjetsCommentairesCount = u.ProjetsCommentaires.Count,
                CommandsCommentairesCount = u.CommandsCommentaires.Count,
                ProjetsCommentaires = expand != null && expand.Contains("projets_commentaires") ? u.ProjetsCommentaires.Take(20).ToList() : null,
                CommandsCommentaires = expand != null && expand.Contains("commands_commentaires") ? u.CommandsCommentaires.Take(20).ToList() : null
            })
            .ToListAsync();
        return user.Select(u =>
        {
            return _mapper.Map<ReadExtendedUserDto>(u.User) with
            {
                projets_commentaires_count = u.ProjetsCommentairesCount,
                commands_commentaires_count = u.CommandsCommentairesCount,
                projets_commentaires = _mapper.Map<IEnumerable<ReadProjetCommentaireDto>>(u.ProjetsCommentaires),
                commands_commentaires = _mapper.Map<IEnumerable<ReadCommandCommentaireDto>>(u.CommandsCommentaires)
            };
        }).ToList();
    }

    public async Task<int> GetUsersCount()
    {
        return await _context.Users.CountAsync();
    }

    public async Task<ReadUserDto> CreateUser(CreateUserDto userDto)
    {
        // if the user is not an admin, he can only create a user with the role "user"
        var User = _sessionService.GetClientRole();
        if (User < UserRole.Admin && userDto.role_user > UserRole.User)
        {
            throw new UnauthorizedAccessException("You are not allowed to create a user with this role");
        }
        // Check if email is already used
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == userDto.email_user);
        if (user is not null)
        {
            throw new InvalidOperationException($"Email {userDto.email_user} is already used");
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
        // send email to the user
        await _smtpService.SendEmailAsync(
            newUser.email_user,
            "Account created",
            "Your account has been created. Your role is " + newUser.role_user.ToString()
        );
        return _mapper.Map<ReadUserDto>(newUser);
    }

    public async Task<ReadUserDto> CreateFirstAdminUser(CreateUserDto userDto)
    {
        // Check if email is already used
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == userDto.email_user);
        if (user is not null)
        {
            throw new InvalidOperationException($"Email {userDto.email_user} is already used");
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
        return _mapper.Map<ReadUserDto>(newUser);
    }

    public async Task<ReadExtendedUserDto> GetUserById(int id, List<string>? expand = null)
    {
        var query = _context.Users.AsQueryable();
        query = query.Where(u => u.id_user == id);
        var user = await query
            .Select(u => new
            {
                User = u,
                ProjetsCommentairesCount = u.ProjetsCommentaires.Count,
                CommandsCommentairesCount = u.CommandsCommentaires.Count,
                ProjetsCommentaires = expand != null && expand.Contains("projets_commentaires") ? u.ProjetsCommentaires.Take(20).ToList() : null,
                CommandsCommentaires = expand != null && expand.Contains("commands_commentaires") ? u.CommandsCommentaires.Take(20).ToList() : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"User with id {id} not found");
        return _mapper.Map<ReadExtendedUserDto>(user.User) with
        {
            projets_commentaires_count = user.ProjetsCommentairesCount,
            commands_commentaires_count = user.CommandsCommentairesCount,
            projets_commentaires = _mapper.Map<IEnumerable<ReadProjetCommentaireDto>>(user.ProjetsCommentaires),
            commands_commentaires = _mapper.Map<IEnumerable<ReadCommandCommentaireDto>>(user.CommandsCommentaires)
        };
    }

    public async Task<ReadUserDto> UpdateUser(int id, UpdateUserDto userDto)
    {
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != id && clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not allowed to update this user");
        }
        var userToUpdate = await _context.Users.FindAsync(id) ?? throw new KeyNotFoundException($"User with id {id} not found");
        if (userDto.nom_user is not null)
        {
            userToUpdate.nom_user = userDto.nom_user;
        }
        if (userDto.prenom_user is not null)
        {
            userToUpdate.prenom_user = userDto.prenom_user;
        }
        var oldUserEmail = userToUpdate.email_user;
        if (userDto.email_user is not null)
        {
            // Check if email is already used
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.email_user == userDto.email_user && u.id_user != id);
            if (existingUser != null)
            {
                throw new InvalidOperationException($"Email {userDto.email_user} is already used by another user");
            }
            userToUpdate.email_user = userDto.email_user;
        }
        if (userDto.mdp_user is not null)
        {
            userToUpdate.mdp_user = BCrypt.Net.BCrypt.HashPassword(userDto.mdp_user);
        }
        if (userDto.role_user is not null)
        {
            if (userDto.role_user == UserRole.Admin && await _context.Users.CountAsync(u => u.role_user == UserRole.Admin) == 1)
            {
                throw new InvalidOperationException("You can't change the role of the last admin");
            }
            userToUpdate.role_user = userDto.role_user ?? userToUpdate.role_user;
        }
        await _context.SaveChangesAsync();
        // Revoke all access tokens and refresh tokens for the user
        await _jwiService.RevokeAllAccessTokenByUser(id, "User update account");
        // send email to the user if the email has changed
        if (userDto.email_user is not null && userToUpdate.email_user != userDto.email_user)
        {
            await _smtpService.SendEmailAsync(
                userToUpdate.email_user,
                "Email changed",
                "Your email has been changed from " + oldUserEmail + " to " + userDto.email_user
            );
            await _smtpService.SendEmailAsync(
                oldUserEmail,
                "Email changed",
                "Your email has been changed from " + oldUserEmail + " to " + userDto.email_user
            );
        }
        else if (userDto.mdp_user is not null)
        {
            await _smtpService.SendEmailAsync(
                userToUpdate.email_user,
                "Password changed",
                "Your password has been changed"
            );
        }
        // send email to the user if any other field has changed
        else
        {
            await _smtpService.SendEmailAsync(
                userToUpdate.email_user,
                "Account updated",
                "Your account has been updated"
            );
        }
        return _mapper.Map<ReadUserDto>(userToUpdate);
    }

    public async Task DeleteUser(int id)
    {
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != id && clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not allowed to delete this user");
        }
        var userToDelete = await _context.Users.FindAsync(id) ?? throw new KeyNotFoundException($"User with id {id} not found");
        if (userToDelete.role_user == UserRole.Admin && await _context.Users.CountAsync(u => u.role_user == UserRole.Admin) == 1)
        {
            throw new InvalidOperationException("You can't delete the last admin");
        }
        await _jwiService.RevokeAllAccessTokenByUser(id, "User delete account");
        await _jwiService.RevokeAllRefreshTokenByUser(id, "User delete account");
        _context.Users.Remove(userToDelete);
        await _context.SaveChangesAsync();
        // send email to the user
        await _smtpService.SendEmailAsync(
            userToDelete.email_user,
            "Account deleted",
            "Your account has been deleted"
        );
    }
}