using AutoMapper;
using ElectrostoreAPI.Constants;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Kafka.Messages;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.JwiService;
using ElectrostoreAPI.Services.SessionService;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ElectrostoreAPI.Services.UserService;

public class UserService : IUserService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IKafkaProducerService _kafkaProducerService;
    private readonly ISessionService _sessionService;
    private readonly IJwiService _jwiService;
    private readonly ILogger<UserService> _logger;
    private readonly string KafkaNotificationTopic = "notification-requests";

    public UserService(IMapper mapper, ApplicationDbContext context, IConfiguration configuration, IKafkaProducerService kafkaNotificationService, ISessionService sessionService, IJwiService jwiService, ILogger<UserService> logger)
    {
        _mapper = mapper;
        _context = context;
        _configuration = configuration;
        _kafkaProducerService = kafkaNotificationService;
        _sessionService = sessionService;
        _jwiService = jwiService;
        _logger = logger;
    }

    public async Task<PaginatedResponseDto<ReadExtendedUserDto>> GetUsers(int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        var expandProjectComments = expand?.Contains("project_comments") == true;
        var expandCommandComments = expand?.Contains("command_comments") == true;
        var query = _context.Users.AsQueryable();
        return await query
            .ToPagedQuery(limit, offset, rsql, sort, new SorterDto { field = "id_user", order = "asc" })
            .ToProjectedResponseAsync(
                u => new
                {
                    User = u,
                    ProjectsCommentsCount = u.ProjectsComments.Count,
                    CommandsCommentsCount = u.CommandsComments.Count,
                    ProjectsComments = expandProjectComments ? u.ProjectsComments.Take(20).ToList() : null,
                    CommandsComments = expandCommandComments ? u.CommandsComments.Take(20).ToList() : null
                },
                user => user.Select(u =>
                {
                    return _mapper.Map<ReadExtendedUserDto>(u.User) with
                    {
                        project_comments_count = u.ProjectsCommentsCount,
                        command_comments_count = u.CommandsCommentsCount,
                        project_comments = _mapper.Map<IEnumerable<ReadProjectCommentDto>>(u.ProjectsComments),
                        command_comments = _mapper.Map<IEnumerable<ReadCommandCommentDto>>(u.CommandsComments)
                    };
                }).ToList());
    }

    public async Task<ReadUserDto> CreateUser(CreateUserDto userDto, bool avoidRoleVerification = false)
    {
        if (!avoidRoleVerification)
        {
            // if the user is not an admin, he can only create a user with the role "user"
            var User = _sessionService.GetClientRole();
            if (User < UserRole.Admin && userDto.role_user > UserRole.User)
            {
                throw new UnauthorizedAccessException("You are not allowed to create a user with this role");
            }
            if (User < UserRole.Admin && !userDto.is_active)
            {
                throw new UnauthorizedAccessException("You are not allowed to create a deactivated user");
            }
        }
        // Check if email is already used
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == userDto.email_user);
        if (user is not null)
        {
            throw new InvalidOperationException($"Email '{userDto.email_user}' is already used");
        }
        var newUser = new Users
        {
            name_user = userDto.name_user,
            firstname_user = userDto.firstname_user,
            email_user = userDto.email_user,
            role_user = userDto.role_user,
            is_active = userDto.is_active
        };
        _context.Users.Add(newUser);
        _context.UsersPasswords.Add(NewPassword(newUser, userDto.password_user));
        await _context.SaveChangesAsync();
        await SendAccountCreatedNotification(newUser);
        await SendEmailVerification(newUser);
        return _mapper.Map<ReadUserDto>(newUser);
    }

    public async Task<(ReadUserDto user, int externalLoginId)> CreateExternalUser(string nameUser, string firstnameUser, string emailUser, UserRole roleUser, string provider, string providerSubject)
    {
        // Check if email is already used
        if (await _context.Users.AnyAsync(u => u.email_user == emailUser))
        {
            throw new InvalidOperationException($"Email '{emailUser}' is already used");
        }
        // the identity provider is the one verifying the email, and the user has no password
        var newUser = new Users
        {
            name_user = nameUser,
            firstname_user = firstnameUser,
            email_user = emailUser,
            role_user = roleUser,
            is_verified = true,
            last_login = DateTime.UtcNow
        };
        var externalLogin = new UsersExternalLogins
        {
            User = newUser,
            provider = provider,
            provider_subject = providerSubject
        };
        _context.Users.Add(newUser);
        _context.UsersExternalLogins.Add(externalLogin);
        await _context.SaveChangesAsync();
        await SendAccountCreatedNotification(newUser);
        return (_mapper.Map<ReadUserDto>(newUser), externalLogin.id_user_external_login);
    }

    public async Task SendEmailVerification(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == email);
        if (user is not null)
        {
            await SendEmailVerification(user);
        }
    }

    // does nothing for a verified user, when SMTP is disabled or when a verification email has just been sent
    private async Task SendEmailVerification(Users user)
    {
        if (user.is_verified || !_configuration.IsSmtpEnabled())
        {
            return;
        }
        var now = DateTime.UtcNow;
        if (user.email_verification_token_sent_at > now - AuthDelays.EmailResendCooldown)
        {
            return;
        }
        user.email_verification_token = Guid.NewGuid();
        user.email_verification_token_expiration = now + AuthDelays.EmailVerificationTokenLifetime;
        user.email_verification_token_sent_at = now;
        await _context.SaveChangesAsync();
        try
        {
            var notification = new NotificationMessage
            {
                RecipientEmail = user.email_user,
                TemplateId = "email-verification",
                Language = _configuration.GetAppLanguage(),
                TemplateValues = new Dictionary<string, string>
                {
                    ["verificationLink"] = _configuration["FrontendUrl"] + "/verify-email?token=" + user.email_verification_token + "&email=" + Uri.EscapeDataString(user.email_user)
                }
            };
            await _kafkaProducerService.PublishAsync(
                KafkaNotificationTopic,
                user.email_user + "-email-verification",
                JsonSerializer.Serialize(notification)
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to send email verification to {Email}", user.email_user);
        }
    }

    private async Task SendAccountCreatedNotification(Users newUser)
    {
        try
        {
            var notification = new NotificationMessage
            {
                RecipientEmail = newUser.email_user,
                TemplateId = "account-created",
                Language = _configuration.GetAppLanguage(),
                TemplateValues = new Dictionary<string, string>
                {
                    ["firstName"] = newUser.firstname_user,
                    ["lastName"] = newUser.name_user,
                    ["role"] = newUser.role_user.ToString()
                }
            };
            await _kafkaProducerService.PublishAsync(
                KafkaNotificationTopic,
                newUser.email_user + "-account-created",
                JsonSerializer.Serialize(notification)
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to send account-created email to {Email}", newUser.email_user);
        }
    }

    private static UsersPasswords NewPassword(Users user, string password)
    {
        return new UsersPasswords
        {
            User = user,
            password_user = BCrypt.Net.BCrypt.HashPassword(password),
            password_last_changed = DateTime.UtcNow
        };
    }

    public async Task<ReadUserDto> CreateFirstAdminUser(CreateUserDto userDto)
    {
        // Check if email is already used
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == userDto.email_user);
        if (user is not null)
        {
            throw new InvalidOperationException($"Email '{userDto.email_user}' is already used");
        }
        var newUser = new Users
        {
            name_user = userDto.name_user,
            firstname_user = userDto.firstname_user,
            email_user = userDto.email_user,
            role_user = userDto.role_user
        };
        _context.Users.Add(newUser);
        _context.UsersPasswords.Add(NewPassword(newUser, userDto.password_user));
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadUserDto>(newUser);
    }

    public async Task<ReadExtendedUserDto> GetUserById(int id, List<string>? expand = null)
    {
        var expandProjectComments = expand?.Contains("project_comments") == true;
        var expandCommandComments = expand?.Contains("command_comments") == true;
        var query = _context.Users.AsQueryable();
        query = query.Where(u => u.id_user == id);
        var user = await query
            .Select(u => new
            {
                User = u,
                ProjectsCommentsCount = u.ProjectsComments.Count,
                CommandsCommentsCount = u.CommandsComments.Count,
                ProjectsComments = expandProjectComments ? u.ProjectsComments.Take(20).ToList() : null,
                CommandsComments = expandCommandComments ? u.CommandsComments.Take(20).ToList() : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"User with id '{id}' not found");
        return _mapper.Map<ReadExtendedUserDto>(user.User) with
        {
            project_comments_count = user.ProjectsCommentsCount,
            command_comments_count = user.CommandsCommentsCount,
            project_comments = _mapper.Map<IEnumerable<ReadProjectCommentDto>>(user.ProjectsComments),
            command_comments = _mapper.Map<IEnumerable<ReadCommandCommentDto>>(user.CommandsComments)
        };
    }

    public async Task<ReadUserDto> UpdateUser(int id, UpdateUserDto userDto)
    {
        await EnsureCanUpdateUser(id, userDto);
        var userToUpdate = await _context.Users.FindAsync(id) ?? throw new KeyNotFoundException($"User with id '{id}' not found");
        if (userDto.name_user is not null)
        {
            userToUpdate.name_user = userDto.name_user;
        }
        if (userDto.firstname_user is not null)
        {
            userToUpdate.firstname_user = userDto.firstname_user;
        }
        var oldUserEmail = userToUpdate.email_user;
        if (userDto.email_user is not null)
        {
            await ApplyEmailUpdate(userToUpdate, userDto.email_user);
        }
        if (userDto.password_user is not null)
        {
            await ApplyPasswordUpdate(userToUpdate, userDto.password_user);
        }
        if (userDto.role_user is not null)
        {
            await ApplyRoleUpdate(userToUpdate, userDto.role_user.Value);
        }
        var deactivated = userDto.is_active is not null && await ApplyActiveUpdate(userToUpdate, userDto.is_active.Value);
        await _context.SaveChangesAsync();
        // Revoke all access tokens and refresh tokens for the user
        await _jwiService.RevokeAllAccessTokenByUser(id, "User update account");
        if (deactivated)
        {
            // a deactivated account can't renew its sessions
            await _jwiService.RevokeAllRefreshTokenByUser(id, "User deactivated");
        }
        // send email to the user if the email has changed
        await AlerteUpdateUser(userToUpdate, userDto, oldUserEmail);
        if (userToUpdate.email_user != oldUserEmail)
        {
            await SendEmailVerification(userToUpdate);
        }
        return _mapper.Map<ReadUserDto>(userToUpdate);
    }

    private async Task ApplyEmailUpdate(Users user, string newEmail)
    {
        // Check if email is already used
        if (await _context.Users.AnyAsync(u => u.email_user == newEmail && u.id_user != user.id_user))
        {
            throw new InvalidOperationException($"Email '{newEmail}' is already used by another user");
        }
        if (user.email_user != newEmail)
        {
            // the new address has to be verified again
            user.is_verified = false;
            user.email_verification_token = null;
            user.email_verification_token_expiration = null;
        }
        user.email_user = newEmail;
    }

    private async Task ApplyPasswordUpdate(Users user, string newPassword)
    {
        // an external login user has no password row until he sets one
        var passwordToUpdate = await _context.UsersPasswords.FindAsync(user.id_user);
        if (passwordToUpdate is null)
        {
            _context.UsersPasswords.Add(NewPassword(user, newPassword));
            return;
        }
        passwordToUpdate.password_user = BCrypt.Net.BCrypt.HashPassword(newPassword);
        passwordToUpdate.password_last_changed = DateTime.UtcNow;
    }

    private async Task ApplyRoleUpdate(Users user, UserRole newRole)
    {
        if (user.role_user == UserRole.Admin && newRole != UserRole.Admin
            && await _context.Users.CountAsync(u => u.role_user == UserRole.Admin) == 1)
        {
            throw new InvalidOperationException("You can't change the role of the last admin");
        }
        user.role_user = newRole;
    }

    // returns true when the account has just been deactivated
    private async Task<bool> ApplyActiveUpdate(Users user, bool isActive)
    {
        if (isActive == user.is_active)
        {
            return false;
        }
        if (_sessionService.GetClientRole() < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not allowed to activate or deactivate an account");
        }
        if (!isActive && user.role_user == UserRole.Admin
            && await _context.Users.CountAsync(u => u.role_user == UserRole.Admin && u.is_active) == 1)
        {
            throw new InvalidOperationException("You can't deactivate the last active admin");
        }
        user.is_active = isActive;
        return !isActive;
    }

    private async Task EnsureCanUpdateUser(int id, UpdateUserDto userDto)
    {
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != id && clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not allowed to update this user");
        }
        var authMethod = _sessionService.GetTokenAuthMethod();
        if (!authMethod.StartsWith("sso_"))
        {
            if (userDto.current_password_user is null)
            {
                throw new InvalidOperationException("Non-SSO users must provide their current password to update an account");
            }
            var currentPassword = await _context.UsersPasswords.FindAsync(clientId);
            if (!currentPassword.IsPasswordValid(userDto.current_password_user))
            {
                throw new InvalidOperationException("Your current password is incorrect");
            }
        }
    }

    public async Task DeleteUser(int id)
    {
        var clientId = _sessionService.GetClientId();
        var clientRole = _sessionService.GetClientRole();
        if (clientId != id && clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not allowed to delete this user");
        }
        var userToDelete = await _context.Users.FindAsync(id) ?? throw new KeyNotFoundException($"User with id '{id}' not found");
        if (userToDelete.role_user == UserRole.Admin && await _context.Users.CountAsync(u => u.role_user == UserRole.Admin) == 1)
        {
            throw new InvalidOperationException("You can't delete the last admin");
        }
        await _jwiService.RevokeAllAccessTokenByUser(id, "User delete account");
        await _jwiService.RevokeAllRefreshTokenByUser(id, "User delete account");
        _context.Users.Remove(userToDelete);
        await _context.SaveChangesAsync();
        // send email to the user
        try
        {
            var notification = new NotificationMessage
            {
                RecipientEmail = userToDelete.email_user,
                TemplateId = "account-deleted",
                Language = _configuration.GetAppLanguage()
            };
            await _kafkaProducerService.PublishAsync(
                KafkaNotificationTopic,
                userToDelete.email_user + "-account-deleted",
                JsonSerializer.Serialize(notification)
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to send account-deleted email to {Email}", userToDelete.email_user);
        }
    }

    public async Task<ReadUserDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsQueryable();
        query = query.Where(u => u.id_user == id);
        var user = await query.FirstOrDefaultAsync(cancellationToken);
        if (user == null)
        {
            return null;
        }
        return _mapper.Map<ReadUserDto>(user);
    }

    public async Task<IEnumerable<ReadUserDto>> GetUsersByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        var users = await _context.Users
            .Where(u => u.role_user == role)
            .OrderBy(u => u.id_user)
            .ToListAsync(cancellationToken);
        return _mapper.Map<IEnumerable<ReadUserDto>>(users);
    }

    private async Task AlerteUpdateUser(Users userToUpdate, UpdateUserDto userDto, string oldUserEmail)
    {
        if (userDto.email_user is not null && oldUserEmail != userDto.email_user)
        {
            try
            {
                var lang = _configuration.GetAppLanguage();
                var values = new Dictionary<string, string>
                {
                    ["oldEmail"] = oldUserEmail,
                    ["newEmail"] = userToUpdate.email_user
                };
                var notificationNew = new NotificationMessage
                {
                        RecipientEmail = userToUpdate.email_user,
                    TemplateId = "email-changed",
                    Language = lang,
                    TemplateValues = values
                };
                await _kafkaProducerService.PublishAsync(
                    KafkaNotificationTopic,
                    userToUpdate.email_user + "-email-changed",
                    JsonSerializer.Serialize(notificationNew)
                );
                var notificationOld = new NotificationMessage
                {
                        RecipientEmail = oldUserEmail,
                    TemplateId = "email-changed",
                    Language = lang,
                    TemplateValues = values
                };
                await _kafkaProducerService.PublishAsync(
                    KafkaNotificationTopic,
                    oldUserEmail + "-email-changed",
                    JsonSerializer.Serialize(notificationOld)
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to send email-changed notification to {NewEmail}/{OldEmail}", userToUpdate.email_user, oldUserEmail);
            }
        }
        else if (userDto.password_user is not null)
        {
            try
            {
                var notification = new NotificationMessage
                {
                        RecipientEmail = userToUpdate.email_user,
                    TemplateId = "password-changed",
                    Language = _configuration.GetAppLanguage()
                };
                await _kafkaProducerService.PublishAsync(
                    KafkaNotificationTopic,
                    userToUpdate.email_user + "-password-changed",
                    JsonSerializer.Serialize(notification)
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to send password-changed notification to {Email}", userToUpdate.email_user);
            }
        }
        else
        {
            try
            {
                var notification = new NotificationMessage
                {
                        RecipientEmail = userToUpdate.email_user,
                    TemplateId = "account-updated",
                    Language = _configuration.GetAppLanguage()
                };
                await _kafkaProducerService.PublishAsync(
                    KafkaNotificationTopic,
                    userToUpdate.email_user + "-account-updated",
                    JsonSerializer.Serialize(notification)
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to send account-updated notification to {Email}", userToUpdate.email_user);
            }
        }
    }
}