using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.JwiService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Services.UserService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class UserServiceTests : TestBase
    {
        private readonly Mock<IKafkaProducerService> _kafkaProducerService = new();
        private readonly Mock<ISessionService> _sessionService = new();
        private readonly Mock<IJwiService> _jwiService = new();

        public UserServiceTests()
        {
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
        }

        private UserService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, new ConfigurationBuilder().Build(), _kafkaProducerService.Object, _sessionService.Object, _jwiService.Object, CreateLogger<UserService>());

        private UserService CreateService(ApplicationDbContext context, IDictionary<string, string?> configuration) =>
            new(_mapper, context, new ConfigurationBuilder().AddInMemoryCollection(configuration).Build(), _kafkaProducerService.Object, _sessionService.Object, _jwiService.Object, CreateLogger<UserService>());

        private static Users BuildUser(string email, UserRole role = UserRole.User) => new()
        {
            name_user = "Nom",
            firstname_user = "Prenom",
            email_user = email,
            role_user = role,
            UsersPasswords = [new UsersPasswords { password_user = BCrypt.Net.BCrypt.HashPassword("Password1!") }]
        };

        private static CreateUserDto BuildCreateDto(string email, UserRole role = UserRole.User) => new()
        {
            name_user = "Nom",
            firstname_user = "Prenom",
            email_user = email,
            password_user = "Password1!",
            role_user = role
        };

        // --- GetUsers ---

        [Fact]
        public async Task GetUsers_ShouldReturnAllUsers_WhenNoFilterApplied()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.AddRange(BuildUser("a@test.com"), BuildUser("b@test.com"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetUsers();

            Assert.Equal(2, result.pagination.total);
        }

        // --- GetUserById ---

        [Fact]
        public async Task GetUserById_ShouldReturnUser_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser("a@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetUserById(user.id_user);

            Assert.Equal(user.id_user, result.id_user);
            Assert.Equal("a@test.com", result.email_user);
        }

        [Fact]
        public async Task GetUserById_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetUserById(999));
        }

        // --- CreateUser ---

        [Fact]
        public async Task CreateUser_ShouldPersistUser_WithHashedPassword()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = BuildCreateDto("new@test.com");

            var result = await service.CreateUser(dto);

            Assert.Equal("new@test.com", result.email_user);
            var persisted = await context.Users.FindAsync(result.id_user);
            Assert.NotNull(persisted);
            Assert.True(persisted!.is_active);
            Assert.False(persisted.is_verified);
            var password = await context.UsersPasswords.FindAsync(result.id_user);
            Assert.NotNull(password);
            Assert.NotEqual("Password1!", password!.password_user);
            Assert.True(BCrypt.Net.BCrypt.Verify("Password1!", password.password_user));
            Assert.NotNull(password.password_last_changed);
        }

        // --- CreateExternalUser ---

        [Fact]
        public async Task CreateExternalUser_ShouldPersistVerifiedUserWithExternalLoginAndNoPassword()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            var (user, externalLoginId) = await service.CreateExternalUser("Family", "Given", "sso@test.com", UserRole.Moderator, "authentik", "sub-1");

            Assert.Equal("sso@test.com", user.email_user);
            Assert.Equal(UserRole.Moderator, user.role_user);
            var persisted = await context.Users.SingleAsync();
            Assert.True(persisted.is_active);
            Assert.True(persisted.is_verified);
            Assert.NotNull(persisted.last_login);
            Assert.Empty(await context.UsersPasswords.ToListAsync());
            var externalLogin = await context.UsersExternalLogins.SingleAsync();
            Assert.Equal(externalLoginId, externalLogin.id_user_external_login);
            Assert.Equal(user.id_user, externalLogin.id_user);
            Assert.Equal("authentik", externalLogin.provider);
            Assert.Equal("sub-1", externalLogin.provider_subject);
            var notification = Assert.Single(SentNotifications());
            Assert.Equal("account-created", notification.Message.TemplateId);
        }

        [Fact]
        public async Task CreateExternalUser_ShouldThrowInvalidOperationException_WhenEmailAlreadyUsed()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser("dup@test.com"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateExternalUser("Family", "Given", "dup@test.com", UserRole.User, "authentik", "sub-1"));
            Assert.Empty(await context.UsersExternalLogins.ToListAsync());
        }

        [Fact]
        public async Task CreateUser_ShouldThrowInvalidOperationException_WhenEmailAlreadyUsed()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser("dup@test.com"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = BuildCreateDto("dup@test.com");

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateUser(dto));
        }

        [Fact]
        public async Task CreateUser_ShouldThrowUnauthorizedAccessException_WhenNonAdminCreatesAdminUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);
            var dto = BuildCreateDto("new@test.com", UserRole.Admin);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateUser(dto));
        }

        [Fact]
        public async Task CreateUser_ShouldBypassRoleVerification_WhenAvoidRoleVerificationIsTrue()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);
            var dto = BuildCreateDto("new@test.com", UserRole.Admin);

            var result = await service.CreateUser(dto, avoidRoleVerification: true);

            Assert.Equal(UserRole.Admin, result.role_user);
        }

        // --- UpdateUser ---

        [Fact]
        public async Task UpdateUser_ShouldUpdateProvidedFields_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser("a@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientId()).Returns(user.id_user);
            _sessionService.Setup(s => s.GetTokenAuthMethod()).Returns("sso_");
            var service = CreateService(context);
            var dto = new UpdateUserDto { name_user = "Renamed" };

            var result = await service.UpdateUser(user.id_user, dto);

            Assert.Equal("Renamed", result.name_user);
            _jwiService.Verify(j => j.RevokeAllAccessTokenByUser(user.id_user, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_ShouldThrowInvalidOperationException_WhenEmailAlreadyUsedByAnotherUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user1 = BuildUser("a@test.com");
            var user2 = BuildUser("b@test.com");
            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientId()).Returns(user1.id_user);
            _sessionService.Setup(s => s.GetTokenAuthMethod()).Returns("sso_");
            var service = CreateService(context);
            var dto = new UpdateUserDto { email_user = "b@test.com" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateUser(user1.id_user, dto));
        }

        [Fact]
        public async Task UpdateUser_ShouldThrowInvalidOperationException_WhenDemotingLastAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var admin = BuildUser("admin@test.com", UserRole.Admin);
            context.Users.Add(admin);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientId()).Returns(admin.id_user);
            _sessionService.Setup(s => s.GetTokenAuthMethod()).Returns("sso_");
            var service = CreateService(context);
            var dto = new UpdateUserDto { role_user = UserRole.User };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateUser(admin.id_user, dto));
        }

        [Fact]
        public async Task UpdateUser_ShouldThrowUnauthorizedAccessException_WhenNonAdminUpdatesAnotherUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser("a@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            _sessionService.Setup(s => s.GetClientId()).Returns(user.id_user + 1);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateUser(user.id_user, new UpdateUserDto { name_user = "x" }));
        }

        [Fact]
        public async Task UpdateUser_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientId()).Returns(1);
            _sessionService.Setup(s => s.GetTokenAuthMethod()).Returns("sso_");
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateUser(999, new UpdateUserDto()));
        }

        // --- DeleteUser ---

        [Fact]
        public async Task DeleteUser_ShouldRemoveUser_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser("a@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientId()).Returns(user.id_user);
            var service = CreateService(context);

            await service.DeleteUser(user.id_user);

            Assert.Equal(0, await context.Users.CountAsync());
        }

        [Fact]
        public async Task DeleteUser_ShouldThrowInvalidOperationException_WhenDeletingLastAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var admin = BuildUser("admin@test.com", UserRole.Admin);
            context.Users.Add(admin);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientId()).Returns(admin.id_user);
            var service = CreateService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteUser(admin.id_user));
            Assert.Equal(1, await context.Users.CountAsync());
        }

        [Fact]
        public async Task DeleteUser_ShouldThrowUnauthorizedAccessException_WhenNonAdminDeletesAnotherUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser("a@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            _sessionService.Setup(s => s.GetClientId()).Returns(user.id_user + 1);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteUser(user.id_user));
        }

        [Fact]
        public async Task DeleteUser_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientId()).Returns(1);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteUser(999));
        }

        // --- additional coverage ---

        private IEnumerable<NotificationMessageView> SentNotifications() =>
            _kafkaProducerService.Invocations
                .Where(i => i.Method.Name == nameof(IKafkaProducerService.PublishAsync))
                .Select(i => new NotificationMessageView((string)i.Arguments[1]!, System.Text.Json.JsonSerializer.Deserialize<ElectrostoreAPI.Kafka.Messages.NotificationMessage>((string)i.Arguments[2]!)!));

        private sealed record NotificationMessageView(string Key, ElectrostoreAPI.Kafka.Messages.NotificationMessage Message);

        private async Task<Users> SeedAuthenticatedUser(ApplicationDbContext context, string email = "a@test.com", string authMethod = "sso_oidc", UserRole role = UserRole.User)
        {
            var user = BuildUser(email, role);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientId()).Returns(user.id_user);
            _sessionService.Setup(s => s.GetTokenAuthMethod()).Returns(authMethod);
            return user;
        }

        [Fact]
        public async Task GetUsers_ShouldRestrictToIds_WhenAnyFilterProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var a = BuildUser("a@test.com");
            var b = BuildUser("b@test.com");
            context.Users.AddRange(a, b);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetUsers(rsql: [new FilterDto { field = "id_user", search_type = "eq", value = b.id_user.ToString() }]);

            var single = Assert.Single(result.data);
            Assert.Equal(b.id_user, single.id_user);
        }

        [Fact]
        public async Task GetUsersAndGetUserById_ShouldReturnCommentCountsAndExpandedComments()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser("a@test.com");
            var project = new Projects { name_project = "p" };
            var command = new Commands { url_command = "https://x", date_command = DateTime.UtcNow };
            context.Users.Add(user);
            context.Projects.Add(project);
            context.Commands.Add(command);
            await context.SaveChangesAsync();
            context.ProjectsComments.Add(new ProjectsComments { id_project = project.id_project, id_user = user.id_user, content_project_comment = "p" });
            context.CommandsComments.Add(new CommandsComments { id_command = command.id_command, id_user = user.id_user, content_command_comment = "c" });
            await context.SaveChangesAsync();
            using var queryContext = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(queryContext);

            var list = await service.GetUsers(expand: ["project_comments", "command_comments"]);
            var byId = await service.GetUserById(user.id_user, ["project_comments", "command_comments"]);

            var single = Assert.Single(list.data);
            Assert.Equal(1, single.project_comments_count);
            Assert.Equal(1, single.command_comments_count);
            Assert.Single(single.project_comments!);
            Assert.Single(single.command_comments!);
            Assert.Single(byId.project_comments!);
            Assert.Single(byId.command_comments!);
        }

        [Fact]
        public async Task CreateUser_ShouldPublishAccountCreatedNotification()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await service.CreateUser(BuildCreateDto("new@test.com"));

            var notification = Assert.Single(SentNotifications(), n => n.Message.TemplateId == "account-created");
            Assert.Equal("new@test.com-account-created", notification.Key);
            Assert.Equal("new@test.com", notification.Message.RecipientEmail);
            Assert.Equal("en", notification.Message.Language);
        }

        [Fact]
        public async Task CreateUser_ShouldSendEmailVerification_AndStoreTheToken()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context, new Dictionary<string, string?> { { "FrontendUrl", "https://app.example.com" } });

            var result = await service.CreateUser(BuildCreateDto("new+tag@test.com"));

            Assert.False(result.is_verified);
            var stored = await context.Users.SingleAsync();
            Assert.NotNull(stored.email_verification_token);
            Assert.True(stored.email_verification_token_expiration > DateTime.UtcNow.AddHours(23));
            Assert.NotNull(stored.email_verification_token_sent_at);
            var notification = Assert.Single(SentNotifications(), n => n.Message.TemplateId == "email-verification");
            Assert.Equal("new+tag@test.com-email-verification", notification.Key);
            Assert.Equal("new+tag@test.com", notification.Message.RecipientEmail);
            Assert.Equal(
                $"https://app.example.com/verify-email?token={stored.email_verification_token}&email=new%2Btag%40test.com",
                notification.Message.TemplateValues!["verificationLink"]);
        }

        [Fact]
        public async Task CreateUser_ShouldNotSendEmailVerification_WhenSmtpIsDisabled()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context, new Dictionary<string, string?> { { "SMTP:Enable", "false" } });

            await service.CreateUser(BuildCreateDto("new@test.com"));

            Assert.DoesNotContain(SentNotifications(), n => n.Message.TemplateId == "email-verification");
            Assert.Null((await context.Users.SingleAsync()).email_verification_token);
        }

        [Fact]
        public async Task CreateUser_ShouldStillSucceed_WhenEmailVerificationCannotBePublished()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _kafkaProducerService
                .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("kafka down"));
            var service = CreateService(context);

            var result = await service.CreateUser(BuildCreateDto("new@test.com"));

            Assert.Equal("new@test.com", result.email_user);
        }

        [Fact]
        public async Task CreateUser_ShouldCreateDeactivatedUser_WhenAdminAsksForIt()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = BuildCreateDto("off@test.com") with { is_active = false };

            var result = await service.CreateUser(dto);

            Assert.False(result.is_active);
            Assert.False((await context.Users.SingleAsync()).is_active);
        }

        [Fact]
        public async Task CreateUser_ShouldCreateActiveUserByDefault()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            var result = await service.CreateUser(BuildCreateDto("on@test.com"));

            Assert.True(result.is_active);
            Assert.Null(result.last_login);
        }

        [Fact]
        public async Task CreateUser_ShouldThrowUnauthorizedAccessException_WhenNonAdminCreatesDeactivatedUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);
            var dto = BuildCreateDto("off@test.com") with { is_active = false };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateUser(dto));
            Assert.Empty(await context.Users.ToListAsync());
        }

        // --- SendEmailVerification ---

        [Fact]
        public async Task SendEmailVerification_ShouldDoNothing_WhenEmailIsUnknownOrAlreadyVerified()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var verified = BuildUser("verified@test.com");
            verified.is_verified = true;
            context.Users.Add(verified);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.SendEmailVerification("unknown@test.com");
            await service.SendEmailVerification("verified@test.com");

            Assert.Empty(SentNotifications());
            Assert.Null((await context.Users.SingleAsync()).email_verification_token);
        }

        [Fact]
        public async Task SendEmailVerification_ShouldNotResend_WithinTheCooldown()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser("a@test.com"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.SendEmailVerification("a@test.com");
            var firstToken = (await context.Users.SingleAsync()).email_verification_token;
            await service.SendEmailVerification("a@test.com");

            Assert.Single(SentNotifications());
            Assert.Equal(firstToken, (await context.Users.SingleAsync()).email_verification_token);
        }

        [Fact]
        public async Task SendEmailVerification_ShouldSendNewTokenAfterTheCooldown()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser("a@test.com");
            user.email_verification_token = Guid.NewGuid();
            user.email_verification_token_sent_at = DateTime.UtcNow.AddMinutes(-5);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var oldToken = user.email_verification_token;
            var service = CreateService(context);

            await service.SendEmailVerification("a@test.com");

            Assert.Single(SentNotifications());
            var stored = await context.Users.SingleAsync();
            Assert.NotEqual(oldToken, stored.email_verification_token);
            Assert.True(stored.email_verification_token_sent_at > DateTime.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public async Task CreateUser_ShouldStillSucceed_WhenNotificationCannotBePublished()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _kafkaProducerService
                .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("kafka down"));
            var service = CreateService(context);

            var result = await service.CreateUser(BuildCreateDto("new@test.com"));

            Assert.Equal("new@test.com", result.email_user);
            Assert.Equal(1, await context.Users.CountAsync());
        }

        [Fact]
        public async Task CreateFirstAdminUser_ShouldPersistUserWithHashedPassword_WithoutNotification()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            var result = await service.CreateFirstAdminUser(BuildCreateDto("root@test.com", UserRole.Admin));

            Assert.Equal(UserRole.Admin, result.role_user);
            Assert.Single(await context.Users.ToListAsync());
            var stored = await context.UsersPasswords.SingleAsync();
            Assert.NotEqual("Password1!", stored.password_user);
            Assert.True(BCrypt.Net.BCrypt.Verify("Password1!", stored.password_user));
            Assert.Empty(SentNotifications());
        }

        [Fact]
        public async Task CreateFirstAdminUser_ShouldThrowInvalidOperationException_WhenEmailAlreadyUsed()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser("root@test.com"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateFirstAdminUser(BuildCreateDto("root@test.com", UserRole.Admin)));
        }

        [Fact]
        public async Task UpdateUser_ShouldRequireCurrentPassword_ForNonSsoUsers()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedAuthenticatedUser(context, authMethod: "local");
            var service = CreateService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateUser(user.id_user, new UpdateUserDto { name_user = "x" }));
        }

        [Fact]
        public async Task UpdateUser_ShouldRejectWrongCurrentPassword_ForNonSsoUsers()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedAuthenticatedUser(context, authMethod: "local");
            var service = CreateService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdateUser(user.id_user, new UpdateUserDto { name_user = "x", current_password_user = "wrong" }));
        }

        [Fact]
        public async Task UpdateUser_ShouldRejectCurrentPassword_WhenUserHasNoPassword()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedAuthenticatedUser(context, authMethod: "local");
            context.UsersPasswords.RemoveRange(context.UsersPasswords);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdateUser(user.id_user, new UpdateUserDto { name_user = "x", current_password_user = "Password1!" }));
        }

        [Fact]
        public async Task UpdateUser_ShouldRejectCurrentPassword_WhenPasswordIsEmpty()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedAuthenticatedUser(context, authMethod: "local");
            (await context.UsersPasswords.SingleAsync()).password_user = string.Empty;
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdateUser(user.id_user, new UpdateUserDto { name_user = "x", current_password_user = string.Empty }));
        }

        [Fact]
        public async Task UpdateUser_ShouldCreatePasswordForExternalLoginUser_WhenPasswordIsSet()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = new Users { name_user = "Nom", firstname_user = "Prenom", email_user = "sso@test.com" };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientId()).Returns(user.id_user);
            _sessionService.Setup(s => s.GetTokenAuthMethod()).Returns("sso_oidc");
            var service = CreateService(context);

            await service.UpdateUser(user.id_user, new UpdateUserDto { password_user = "NewPassword1!" });

            var password = await context.UsersPasswords.SingleAsync();
            Assert.Equal(user.id_user, password.id_user);
            Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword1!", password.password_user));
            Assert.NotNull(password.password_last_changed);
        }

        [Fact]
        public async Task UpdateUser_ShouldAcceptCorrectCurrentPassword_ForNonSsoUsers()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedAuthenticatedUser(context, authMethod: "local");
            var service = CreateService(context);

            var result = await service.UpdateUser(user.id_user, new UpdateUserDto { name_user = "x", current_password_user = "Password1!" });

            Assert.Equal("x", result.name_user);
        }

        [Fact]
        public async Task UpdateUser_ShouldUpdateEveryField_AndSendEmailChangedNotificationsToBothAddresses()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedAuthenticatedUser(context, "old@test.com");
            var service = CreateService(context);

            var result = await service.UpdateUser(user.id_user, new UpdateUserDto
            {
                name_user = "N",
                firstname_user = "F",
                email_user = "new@test.com",
                password_user = "NewPassword1!",
                role_user = UserRole.Moderator
            });

            Assert.Equal("N", result.name_user);
            Assert.Equal("F", result.firstname_user);
            Assert.Equal("new@test.com", result.email_user);
            Assert.Equal(UserRole.Moderator, result.role_user);
            Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword1!", (await context.UsersPasswords.FindAsync(user.id_user))!.password_user));
            var notifications = SentNotifications().Where(n => n.Message.TemplateId == "email-changed").ToList();
            Assert.Equal(2, notifications.Count);
            Assert.Contains(notifications, n => n.Message.RecipientEmail == "new@test.com");
            Assert.Contains(notifications, n => n.Message.RecipientEmail == "old@test.com");
            // the new address has to be verified
            var verification = Assert.Single(SentNotifications(), n => n.Message.TemplateId == "email-verification");
            Assert.Equal("new@test.com", verification.Message.RecipientEmail);
            Assert.False(result.is_verified);
        }

        [Fact]
        public async Task UpdateUser_ShouldResetVerification_WhenEmailChanges()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedAuthenticatedUser(context, "old@test.com");
            user.is_verified = true;
            user.email_verification_token = Guid.NewGuid();
            await context.SaveChangesAsync();
            var oldToken = user.email_verification_token;
            var service = CreateService(context);

            var result = await service.UpdateUser(user.id_user, new UpdateUserDto { email_user = "new@test.com" });

            Assert.False(result.is_verified);
            var stored = await context.Users.SingleAsync();
            Assert.NotNull(stored.email_verification_token);
            Assert.NotEqual(oldToken, stored.email_verification_token);
        }

        [Fact]
        public async Task UpdateUser_ShouldKeepVerification_WhenEmailIsUnchanged()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedAuthenticatedUser(context, "same@test.com");
            user.is_verified = true;
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateUser(user.id_user, new UpdateUserDto { email_user = "same@test.com", name_user = "x" });

            Assert.True(result.is_verified);
            Assert.DoesNotContain(SentNotifications(), n => n.Message.TemplateId == "email-verification");
        }

        // --- UpdateUser: is_active ---

        [Fact]
        public async Task UpdateUser_ShouldDeactivateUser_AndRevokeItsSessions()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var admin = await SeedAuthenticatedUser(context, "admin@test.com", role: UserRole.Admin);
            var target = BuildUser("target@test.com");
            context.Users.Add(target);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateUser(target.id_user, new UpdateUserDto { is_active = false });

            Assert.False(result.is_active);
            Assert.False((await context.Users.FindAsync(target.id_user))!.is_active);
            _jwiService.Verify(j => j.RevokeAllRefreshTokenByUser(target.id_user, "User deactivated"), Times.Once);
            _jwiService.Verify(j => j.RevokeAllAccessTokenByUser(target.id_user, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_ShouldReactivateUser_WithoutRevokingTheRefreshTokens()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            await SeedAuthenticatedUser(context, "admin@test.com", role: UserRole.Admin);
            var target = BuildUser("target@test.com");
            target.is_active = false;
            context.Users.Add(target);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateUser(target.id_user, new UpdateUserDto { is_active = true });

            Assert.True(result.is_active);
            _jwiService.Verify(j => j.RevokeAllRefreshTokenByUser(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateUser_ShouldThrowUnauthorizedAccessException_WhenNonAdminChangesIsActive()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedAuthenticatedUser(context, "user@test.com");
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateUser(user.id_user, new UpdateUserDto { is_active = false }));
            Assert.True((await context.Users.FindAsync(user.id_user))!.is_active);
        }

        [Fact]
        public async Task UpdateUser_ShouldAcceptUnchangedIsActive_FromNonAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedAuthenticatedUser(context, "user@test.com");
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            var result = await service.UpdateUser(user.id_user, new UpdateUserDto { is_active = true, name_user = "x" });

            Assert.True(result.is_active);
            Assert.Equal("x", result.name_user);
        }

        [Fact]
        public async Task UpdateUser_ShouldThrowInvalidOperationException_WhenDeactivatingTheLastActiveAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var admin = await SeedAuthenticatedUser(context, "admin@test.com", role: UserRole.Admin);
            var inactiveAdmin = BuildUser("other-admin@test.com", UserRole.Admin);
            inactiveAdmin.is_active = false;
            context.Users.Add(inactiveAdmin);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateUser(admin.id_user, new UpdateUserDto { is_active = false }));
            Assert.True((await context.Users.FindAsync(admin.id_user))!.is_active);
        }

        [Fact]
        public async Task UpdateUser_ShouldSendPasswordChangedNotification_WhenOnlyPasswordChanges()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedAuthenticatedUser(context);
            var service = CreateService(context);

            await service.UpdateUser(user.id_user, new UpdateUserDto { password_user = "NewPassword1!" });

            var notification = Assert.Single(SentNotifications());
            Assert.Equal("password-changed", notification.Message.TemplateId);
            Assert.Equal("a@test.com-password-changed", notification.Key);
        }

        [Fact]
        public async Task UpdateUser_ShouldSendAccountUpdatedNotification_WhenOtherFieldsChange()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedAuthenticatedUser(context);
            var service = CreateService(context);

            await service.UpdateUser(user.id_user, new UpdateUserDto { name_user = "Renamed" });

            var notification = Assert.Single(SentNotifications());
            Assert.Equal("account-updated", notification.Message.TemplateId);
        }

        [Fact]
        public async Task UpdateUser_ShouldNotSendEmailChangedNotification_WhenEmailIsUnchanged()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedAuthenticatedUser(context);
            var service = CreateService(context);

            await service.UpdateUser(user.id_user, new UpdateUserDto { email_user = "a@test.com" });

            var notification = Assert.Single(SentNotifications());
            Assert.Equal("account-updated", notification.Message.TemplateId);
        }

        [Fact]
        public async Task UpdateUser_ShouldStillSucceed_WhenNotificationsCannotBePublished()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedAuthenticatedUser(context, "old@test.com");
            _kafkaProducerService
                .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("kafka down"));
            var service = CreateService(context);

            var emailChange = await service.UpdateUser(user.id_user, new UpdateUserDto { email_user = "new@test.com" });
            var passwordChange = await service.UpdateUser(user.id_user, new UpdateUserDto { password_user = "NewPassword1!" });
            var otherChange = await service.UpdateUser(user.id_user, new UpdateUserDto { name_user = "x" });

            Assert.Equal("new@test.com", emailChange.email_user);
            Assert.NotNull(passwordChange);
            Assert.Equal("x", otherChange.name_user);
        }

        [Fact]
        public async Task UpdateUser_ShouldAllowDemotingAnAdmin_WhenAnotherAdminExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var admin = await SeedAuthenticatedUser(context, "admin@test.com", role: UserRole.Admin);
            context.Users.Add(BuildUser("other-admin@test.com", UserRole.Admin));
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
            var service = CreateService(context);

            var result = await service.UpdateUser(admin.id_user, new UpdateUserDto { role_user = UserRole.User });

            Assert.Equal(UserRole.User, result.role_user);
        }

        [Fact]
        public async Task DeleteUser_ShouldRevokeTokens_AndSendAccountDeletedNotification()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser("a@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientId()).Returns(user.id_user + 100);
            var service = CreateService(context);

            await service.DeleteUser(user.id_user);

            _jwiService.Verify(j => j.RevokeAllAccessTokenByUser(user.id_user, It.IsAny<string>()), Times.Once);
            _jwiService.Verify(j => j.RevokeAllRefreshTokenByUser(user.id_user, It.IsAny<string>()), Times.Once);
            var notification = Assert.Single(SentNotifications());
            Assert.Equal("account-deleted", notification.Message.TemplateId);
            Assert.Equal("a@test.com-account-deleted", notification.Key);
        }

        [Fact]
        public async Task DeleteUser_ShouldStillSucceed_WhenNotificationCannotBePublished()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser("a@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientId()).Returns(user.id_user);
            _kafkaProducerService
                .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("kafka down"));
            var service = CreateService(context);

            await service.DeleteUser(user.id_user);

            Assert.Equal(0, await context.Users.CountAsync());
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnUser_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser("a@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetUserByIdAsync(user.id_user);

            Assert.Equal("a@test.com", result!.email_user);
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            Assert.Null(await service.GetUserByIdAsync(999));
        }

        [Fact]
        public async Task GetUsersByRoleAsync_ShouldReturnOnlyUsersOfRole_OrderedById()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var admin1 = BuildUser("a1@test.com", UserRole.Admin);
            var user = BuildUser("u@test.com");
            var admin2 = BuildUser("a2@test.com", UserRole.Admin);
            context.Users.AddRange(admin1, user, admin2);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = (await service.GetUsersByRoleAsync(UserRole.Admin)).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal([admin1.id_user, admin2.id_user], result.Select(u => u.id_user));
        }
    }
}
