using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using System.Net;
using System.Net.Mail;

namespace electrostore.Services.UserService;

public class UserService : IUserService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public UserService(IMapper mapper, ApplicationDbContext context, IConfiguration configuration)
    {
        _mapper = mapper;
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
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("projets_commentaires"))
        {
            query = query.Include(u => u.ProjetsCommentaires);
        }
        if (expand != null && expand.Contains("commands_commentaires"))
        {
            query = query.Include(u => u.CommandsCommentaires);
        }
        var user = await query.ToListAsync();
        return _mapper.Map<List<ReadExtendedUserDto>>(user);
    }

    public async Task<int> GetUsersCount()
    {
        return await _context.Users.CountAsync();
    }

    public async Task<ReadUserDto> CreateUser(CreateUserDto userDto)
    {
        // check if the db is empty, if it is, create an admin user
        Users newUser;
        if (!await _context.Users.AnyAsync())
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
        return _mapper.Map<ReadUserDto>(newUser);
    }

    public async Task<ReadExtendedUserDto> GetUserById(int id, List<string>? expand = null)
    {
        var query = _context.Users.AsQueryable();
        query = query.Where(u => u.id_user == id);
        if (expand != null && expand.Contains("projets_commentaires"))
        {
            query = query.Include(u => u.ProjetsCommentaires);
        }
        if (expand != null && expand.Contains("commands_commentaires"))
        {
            query = query.Include(u => u.CommandsCommentaires);
        }
        var user = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"User with id {id} not found");
        return _mapper.Map<ReadExtendedUserDto>(user);
    }

    public async Task<ReadUserDto> GetUserByEmail(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == email) ?? throw new KeyNotFoundException($"User with email {email} not found");
        return _mapper.Map<ReadUserDto>(user);
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
            if (userToUpdate.role_user == "admin" && await _context.Users.CountAsync(u => u.role_user == "admin") == 1)
            {
                throw new InvalidOperationException("You can't change the role of the last admin");
            }
            userToUpdate.role_user = userDto.role_user;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadUserDto>(userToUpdate);
    }

    public async Task DeleteUser(int id)
    {
        var userToDelete = await _context.Users.FindAsync(id) ?? throw new KeyNotFoundException($"User with id {id} not found");
        if (userToDelete.role_user == "admin" && await _context.Users.CountAsync(u => u.role_user == "admin") == 1)
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

    public async Task<ReadUserDto> ResetPassword(ResetPasswordRequest request)
    {
        //check if SMTP is Enabled
        if (_configuration["SMTP:Enable"] != "true")
        {
            throw new InvalidOperationException("SMTP is not enabled");
        }
        // check if token is valid
        var user = await _context.Users.FirstOrDefaultAsync(
            u => u.email_user == request.Email && u.reset_token.ToString() == request.Token && u.reset_token_expiration > DateTime.Now
        ) ?? throw new InvalidOperationException("Invalid token");
        // update password
        user.mdp_user = BCrypt.Net.BCrypt.HashPassword(request.Password);
        user.reset_token = null;
        user.reset_token_expiration = null;
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadUserDto>(user);
    }
}