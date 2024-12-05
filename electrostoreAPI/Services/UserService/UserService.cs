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

    public async Task<int> GetUsersCount()
    {
        return await _context.Users.CountAsync();
    }

    public async Task<ReadUserDto> CreateUser(CreateUserDto userDto)
    {
        var newUser = new Users();
        // check email format
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(userDto.email_user))
        {
            throw new InvalidOperationException("Invalid email format");
        }
        // check password length and if it contain a number and a special character and a uppercase letter and a lowercase letter and if it's at least 8 characters long
        if (!new System.ComponentModel.DataAnnotations.RegularExpressionAttribute(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$").IsValid(userDto.mdp_user))
        {
            throw new InvalidOperationException("password must contain a number and a special character and a uppercase letter and a lowercase letter and if it's at least 8 characters long");
        }
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
            var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == userDto.email_user);
            if (user != null)
            {
                throw new InvalidOperationException("Email already used");
            }
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

    public async Task<ReadUserDto> GetUserById(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with id {id} not found");
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
            throw new KeyNotFoundException($"User with email {email} not found");
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
        var userToUpdate = await _context.Users.FindAsync(id) ?? throw new KeyNotFoundException($"User with id {id} not found");
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
                throw new InvalidOperationException("Invalid email format");
            }
            // Check if email is already used
            var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == userDto.email_user);
            if (user != null)
            {
                throw new InvalidOperationException("Email already used");
            }
            userToUpdate.email_user = userDto.email_user;
        }
        if (userDto.mdp_user != null)
        {
            // check password length and if it contain a number and a special character and a uppercase letter and a lowercase letter and if it's at least 8 characters long
            if (!new System.ComponentModel.DataAnnotations.RegularExpressionAttribute(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$").IsValid(userDto.mdp_user))
            {
                throw new InvalidOperationException("password must contain a number and a special character and a uppercase letter and a lowercase letter and if it's at least 8 characters long");
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

    public async Task DeleteUser(int id)
    {
        var userToDelete = await _context.Users.FindAsync(id) ?? throw new KeyNotFoundException($"User with id {id} not found");
        _context.Users.Remove(userToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> CheckUserPasswordByEmail(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == email);
        if (user == null)
        {
            return false;
        }
        return BCrypt.Net.BCrypt.Verify(password, user.mdp_user);
    }

    public async Task<bool> CheckUserPasswordById(int id, string password)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
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
        if (user != null)
        {
            // add reset_token
            user.reset_token = Guid.NewGuid().ToString();
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
                Body = "Click on the following link to reset your password: " + _configuration["FrontendUrl"] + "/reset-password?token=" + user.reset_token + "&email=" + user.email_user,
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
            u => u.email_user == resetPasswordRequest.Email && u.reset_token == resetPasswordRequest.Token && u.reset_token_expiration > DateTime.Now
        ) ?? throw new InvalidOperationException("Invalid token");
        // check password length and if it contain a number and a special character and a uppercase letter and a lowercase letter and if it's at least 8 characters long
        if (!new System.ComponentModel.DataAnnotations.RegularExpressionAttribute(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$").IsValid(resetPasswordRequest.Password))
        {
            throw new InvalidOperationException("password must contain a number and a special character and a uppercase letter and a lowercase letter and if it's at least 8 characters long");
        }
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