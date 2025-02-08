using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;

namespace electrostore.Services.UserService;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public UserService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<IEnumerable<ReadExtendedUserDto>> GetUsers(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Users.AsQueryable();
        if (idResearch != null)
        {
            query = query.Where(b => idResearch.Contains(b.id_user));
        }
        return await query
            .Skip(offset)
            .Take(limit)
            .Select(u => new ReadExtendedUserDto
            {
                id_user = u.id_user,
                nom_user = u.nom_user,
                prenom_user = u.prenom_user,
                email_user = u.email_user,
                role_user = u.role_user,
                projets_commentaires = expand != null && expand.Contains("projets_commentaires") ? u.ProjetsCommentaires
                    .Select(pc => new ReadProjetCommentaireDto
                    {
                        id_projet_commentaire = pc.id_projet_commentaire,
                        contenu_projet_commentaire = pc.contenu_projet_commentaire,
                        date_projet_commentaire = pc.date_projet_commentaire,
                        id_user = pc.id_user,
                        id_projet = pc.id_projet
                    })
                    .ToArray() : null,
                commands_commentaires = expand != null && expand.Contains("commands_commentaires") ? u.CommandsCommentaires
                    .Select(cc => new ReadCommandCommentaireDto
                    {
                        id_command_commentaire = cc.id_command_commentaire,
                        contenu_command_commentaire = cc.contenu_command_commentaire,
                        date_command_commentaire = cc.date_command_commentaire,
                        id_user = cc.id_user,
                        id_command = cc.id_command
                    })
                    .ToArray() : null,
                projets_commentaires_count = u.ProjetsCommentaires.Count,
                commands_commentaires_count = u.CommandsCommentaires.Count
            })
            .ToListAsync();
    }

    public async Task<int> GetUsersCount()
    {
        return await _context.Users.CountAsync();
    }

    public async Task<ReadUserDto> CreateUser(CreateUserDto userDto)
    {
        var newUser = new Users();
        // check if the db is empty, if it is, create an admin user
        if (!_context.Users.Any())
        {
            newUser = new Users
            {
                nom_user = userDto.nom_user,
                prenom_user = userDto.prenom_user,
                email_user = userDto.email_user,
                mdp_user = BCrypt.Net.BCrypt.HashPassword(userDto.mdp_user),
                role_user = "admin"
            };
        } else {
            // Check if email is already used
            var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == userDto.email_user) ?? throw new InvalidOperationException($"Email {userDto.email_user} is already used");
            newUser = new Users
            {
                nom_user = userDto.nom_user,
                prenom_user = userDto.prenom_user,
                email_user = userDto.email_user,
                mdp_user = BCrypt.Net.BCrypt.HashPassword(userDto.mdp_user),
                role_user = userDto.role_user
            };
        }

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

    public async Task<ReadExtendedUserDto> GetUserById(int id, List<string>? expand = null)
    {
        return await _context.Users
            .Where(u => u.id_user == id)
            .Select(u => new ReadExtendedUserDto
            {
                id_user = u.id_user,
                nom_user = u.nom_user,
                prenom_user = u.prenom_user,
                email_user = u.email_user,
                role_user = u.role_user,
                projets_commentaires = expand != null && expand.Contains("projets_commentaires") ? u.ProjetsCommentaires
                    .Select(pc => new ReadProjetCommentaireDto
                    {
                        id_projet_commentaire = pc.id_projet_commentaire,
                        contenu_projet_commentaire = pc.contenu_projet_commentaire,
                        date_projet_commentaire = pc.date_projet_commentaire,
                        id_user = pc.id_user,
                        id_projet = pc.id_projet
                    })
                    .ToArray() : null,
                commands_commentaires = expand != null && expand.Contains("commands_commentaires") ? u.CommandsCommentaires
                    .Select(cc => new ReadCommandCommentaireDto
                    {
                        id_command_commentaire = cc.id_command_commentaire,
                        contenu_command_commentaire = cc.contenu_command_commentaire,
                        date_command_commentaire = cc.date_command_commentaire,
                        id_user = cc.id_user,
                        id_command = cc.id_command
                    })
                    .ToArray() : null,
                projets_commentaires_count = u.ProjetsCommentaires.Count,
                commands_commentaires_count = u.CommandsCommentaires.Count
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"User with id {id} not found");
    }

    public async Task<ReadUserDto> GetUserByEmail(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == email) ?? throw new KeyNotFoundException($"User with email {email} not found");
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
        var userToUpdate = await _context.Users.FindAsync(id) ?? throw new KeyNotFoundException($"User with id {id} not found");
        if (userDto.nom_user is not null)
        {
            userToUpdate.nom_user = userDto.nom_user;
        }
        if (userDto.prenom_user is not null)
        {
            userToUpdate.prenom_user = userDto.prenom_user;
        }
        if (userDto.email_user is not null)
        {
            // Check if email is already used
            var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == userDto.email_user) ?? throw new KeyNotFoundException($"User with email {userDto.email_user} not found");
            userToUpdate.email_user = userDto.email_user;
        }
        if (userDto.mdp_user is not null)
        {
            userToUpdate.mdp_user = BCrypt.Net.BCrypt.HashPassword(userDto.mdp_user);
        }
        if (userDto.role_user is not null)
        {
            if (userToUpdate.role_user == "admin" && _context.Users.Count(u => u.role_user == "admin") == 1)
            {
                throw new InvalidOperationException("You can't change the role of the last admin");
            }
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
        var userToDelete = await _context.Users.FindAsync(id) ?? throw new KeyNotFoundException($"User with id {id} not found");
        if (userToDelete.role_user == "admin" && _context.Users.Count(u => u.role_user == "admin") == 1)
        {
            throw new InvalidOperationException("You can't delete the last admin");
        }
        _context.Users.Remove(userToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> CheckUserPasswordByEmail(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == email) ?? throw new KeyNotFoundException($"User with email {email} not found");
        return BCrypt.Net.BCrypt.Verify(password, user.mdp_user);
    }

    public async Task<bool> CheckUserPasswordById(int id, string password)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null)
        {
            return false;
        }
        return BCrypt.Net.BCrypt.Verify(password, user.mdp_user);
    }

    public async Task ForgotPassword(ForgotPasswordRequest request)
    {
        //check if SMTP is Enabled
        if (_configuration["SMTP:Enable"] != "true")
        {
            throw new InvalidOperationException("SMTP is not enabled");
        }
        // check if user exists
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == request.Email);
        if (user is not null)
        {
            // add reset_token
            user.reset_token = Guid.NewGuid();
            user.reset_token_expiration = DateTime.Now.AddHours(1);
            await _context.SaveChangesAsync();
            // send email with reset_token
            var smtpClient = new SmtpClient(_configuration["SMTP:Host"])
            {
                Port = int.Parse(_configuration["SMTP:Port"] ?? "587"),
                Credentials = new NetworkCredential(_configuration["SMTP:Username"], _configuration["SMTP:Password"]),
                EnableSsl = true
            };
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_configuration["SMTP:Username"]),
                Subject = "Reset password",
                Body = "Click on the following link to reset your password: " + _configuration["FrontendUrl"] + "/reset-password?token=" + user.reset_token.ToString() + "&email=" + user.email_user,
                IsBodyHtml = true
            };
            mailMessage.To.Add(request.Email);
            // send email
            var sendEmailTask = smtpClient.SendMailAsync(mailMessage);
            await sendEmailTask;
            if (sendEmailTask.IsFaulted)
            {
                // server error
                throw new InvalidOperationException("An error occured while sending the email");
            }
        }
    }

    public async Task<ReadUserDto> ResetPassword(ResetPasswordRequest resetPasswordRequest)
    {
        //check if SMTP is Enabled
        if (_configuration["SMTP:Enable"] != "true")
        {
            throw new InvalidOperationException("SMTP is not enabled");
        }
        // check if token is valid
        var user = await _context.Users.FirstOrDefaultAsync(
            u => u.email_user == resetPasswordRequest.Email && u.reset_token.ToString() == resetPasswordRequest.Token && u.reset_token_expiration > DateTime.Now
        ) ?? throw new InvalidOperationException("Invalid token");
        // update password
        user.mdp_user = BCrypt.Net.BCrypt.HashPassword(resetPasswordRequest.Password);
        user.reset_token = null;
        user.reset_token_expiration = null;
        await _context.SaveChangesAsync();
        return new ReadUserDto
        {
            id_user = user.id_user,
            nom_user = user.nom_user,
            prenom_user = user.prenom_user,
            email_user = user.email_user,
            role_user = user.role_user
        };
    }
}