using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Kafka.Messages;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.JwiService;
using ElectrostoreAPI.Services.SessionService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
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

    public UserService(IMapper mapper, ApplicationDbContext context, IConfiguration configuration, IKafkaProducerService kafkaNotificationService, ISessionService sessionService, IJwiService jwiService)
    {
        _mapper = mapper;
        _context = context;
        _configuration = configuration;
        _kafkaProducerService = kafkaNotificationService;
        _sessionService = sessionService;
        _jwiService = jwiService;
    }

    public async Task<PaginatedResponseDto<ReadExtendedUserDto>> GetUsers(int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Users.AsQueryable();
        var filterResult = default(Expression<Func<Users, bool>>);
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(u => idResearch.Contains(u.id_user));
        }
        else
        {
            if (rsql != null && rsql.Count > 0)
            {
                (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<Users>(rsql);
                query = query.Where(filterResult);
            }
            if (!string.IsNullOrEmpty(sort?.Field))
            {
                var sortResult = RsqlParserExtensions.ToSortExpression<Users>(sort);
                if (sortResult.Item1 != null)
                {
                    query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
                }
                else
                {
                    sort = new SorterDto { Field = "id_user", Order = "asc" };
                    query = query.OrderBy(u => u.id_user);
                }
            }
            else
            {
                query = query.OrderBy(u => u.id_user);
            }
        }
        query = query.Skip(offset).Take(limit);
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
        return new PaginatedResponseDto<ReadExtendedUserDto>
        {
            data = user.Select(u =>
            {
                return _mapper.Map<ReadExtendedUserDto>(u.User) with
                {
                    projets_commentaires_count = u.ProjetsCommentairesCount,
                    commands_commentaires_count = u.CommandsCommentairesCount,
                    projets_commentaires = _mapper.Map<IEnumerable<ReadProjetCommentaireDto>>(u.ProjetsCommentaires),
                    commands_commentaires = _mapper.Map<IEnumerable<ReadCommandCommentaireDto>>(u.CommandsCommentaires)
                };
            }).ToList(),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.Users.CountAsync(filterResult ?? (u => true)),
                nextOffset = offset + limit,
                hasMore = await _context.Users.Skip(offset + limit).AnyAsync(filterResult ?? (u => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
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
        }
        // Check if email is already used
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == userDto.email_user);
        if (user is not null)
        {
            throw new InvalidOperationException($"Email '{userDto.email_user}' is already used");
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
        try
        {
            var notification = new NotificationMessage
            {
                Types = ["email"],
                RecipientEmail = newUser.email_user,
                TemplateId = "account-created",
                Language = _configuration.GetValue<string>("AppLanguage") ?? "fr",
                TemplateValues = new Dictionary<string, string>
                {
                    ["firstName"] = newUser.prenom_user,
                    ["lastName"] = newUser.nom_user,
                    ["role"] = newUser.role_user.ToString()
                }
            };
            await _kafkaProducerService.PublishAsync(
                "notification-requests",
                newUser.email_user + "-account-created",
                JsonSerializer.Serialize(notification)
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SMTP Error: Unable to send login notification email - {ex.Message}");
        }
        return _mapper.Map<ReadUserDto>(newUser);
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
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"User with id '{id}' not found");
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
        var userToUpdate = await _context.Users.FindAsync(id) ?? throw new KeyNotFoundException($"User with id '{id}' not found");
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
                throw new InvalidOperationException($"Email '{userDto.email_user}' is already used by another user");
            }
            userToUpdate.email_user = userDto.email_user;
        }
        if (userDto.mdp_user is not null)
        {
            userToUpdate.mdp_user = BCrypt.Net.BCrypt.HashPassword(userDto.mdp_user);
        }
        if (userDto.role_user is not null)
        {
            if (userToUpdate.role_user == UserRole.Admin && userDto.role_user != UserRole.Admin && await _context.Users.CountAsync(u => u.role_user == UserRole.Admin) == 1)
            {
                throw new InvalidOperationException("You can't change the role of the last admin");
            }
            userToUpdate.role_user = userDto.role_user ?? userToUpdate.role_user;
        }
        await _context.SaveChangesAsync();
        // Revoke all access tokens and refresh tokens for the user
        await _jwiService.RevokeAllAccessTokenByUser(id, "User update account");
        // send email to the user if the email has changed
        await AlerteUpdateUser(userToUpdate, userDto, oldUserEmail);
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
                Types = ["email"],
                RecipientEmail = userToDelete.email_user,
                TemplateId = "account-deleted",
                Language = _configuration.GetValue<string>("AppLanguage") ?? "fr"
            };
            await _kafkaProducerService.PublishAsync(
                "notification-requests",
                userToDelete.email_user + "-account-deleted",
                JsonSerializer.Serialize(notification)
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SMTP Error: Unable to send login notification email - {ex.Message}");
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

    private async Task AlerteUpdateUser(Users userToUpdate, UpdateUserDto userDto, string oldUserEmail)
    {
        if (userDto.email_user is not null && oldUserEmail != userDto.email_user)
        {
            try
            {
                var lang = _configuration.GetValue<string>("AppLanguage") ?? "fr";
                var values = new Dictionary<string, string>
                {
                    ["oldEmail"] = oldUserEmail,
                    ["newEmail"] = userToUpdate.email_user
                };
                var notificationNew = new NotificationMessage
                {
                    Types = ["email"],
                    RecipientEmail = userToUpdate.email_user,
                    TemplateId = "email-changed",
                    Language = lang,
                    TemplateValues = values
                };
                await _kafkaProducerService.PublishAsync(
                    "notification-requests",
                    userToUpdate.email_user + "-email-changed",
                    JsonSerializer.Serialize(notificationNew)
                );
                var notificationOld = new NotificationMessage
                {
                    Types = ["email"],
                    RecipientEmail = oldUserEmail,
                    TemplateId = "email-changed",
                    Language = lang,
                    TemplateValues = values
                };
                await _kafkaProducerService.PublishAsync(
                    "notification-requests",
                    oldUserEmail + "-email-changed",
                    JsonSerializer.Serialize(notificationOld)
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SMTP Error: Unable to send login notification email - {ex.Message}");
            }
        }
        else if (userDto.mdp_user is not null)
        {
            try
            {
                var notification = new NotificationMessage
                {
                    Types = ["email"],
                    RecipientEmail = userToUpdate.email_user,
                    TemplateId = "password-changed",
                    Language = _configuration.GetValue<string>("AppLanguage") ?? "fr"
                };
                await _kafkaProducerService.PublishAsync(
                    "notification-requests",
                    userToUpdate.email_user + "-password-changed",
                    JsonSerializer.Serialize(notification)
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SMTP Error: Unable to send login notification email - {ex.Message}");
            }
        }
        else
        {
            try
            {
                var notification = new NotificationMessage
                {
                    Types = ["email"],
                    RecipientEmail = userToUpdate.email_user,
                    TemplateId = "account-updated",
                    Language = _configuration.GetValue<string>("AppLanguage") ?? "fr"
                };
                await _kafkaProducerService.PublishAsync(
                    "notification-requests",
                    userToUpdate.email_user + "-account-updated",
                    JsonSerializer.Serialize(notification)
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SMTP Error: Unable to send login notification email - {ex.Message}");
            }
        }
    }
}