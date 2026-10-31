using AutoMapper;
using ElectrostoreAPI.Constants;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Kafka.Messages;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Services.UserService;
using ElectrostoreAPI.Services.JwiService;
using ElectrostoreAPI.Services.JwtService;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace ElectrostoreAPI.Services.AuthService;

public class AuthService : IAuthService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IKafkaProducerService _kafkaProducerService;
    private readonly ISessionService _sessionService;
    private readonly IUserService _userService;
    private readonly IJwtService _jwtService;
    private readonly IJwiService _jwiService;
    private readonly ILogger<AuthService> _logger;
    private readonly string KafkaNotificationTopic = "notification-requests";
    private static readonly Dictionary<string, DateTime> _stateStore = new();
    private static readonly char[] separator = new char[] { '_', '-' };
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    // In-memory store for state parameters, if you want persistence or use duplication across instances, consider using a distributed cache like Redis

    public AuthService(IMapper mapper, ApplicationDbContext context, IConfiguration configuration, IKafkaProducerService kafkaNotificationService, ISessionService sessionService, IUserService userService, IJwtService jwtService, IJwiService jwiService, ILogger<AuthService> logger)
    {
        _mapper = mapper;
        _context = context;
        _configuration = configuration;
        _kafkaProducerService = kafkaNotificationService;
        _sessionService = sessionService;
        _userService = userService;
        _jwtService = jwtService;
        _jwiService = jwiService;
        _logger = logger;
    }

    public async Task<SsoUrlResponse> GetSSOAuthUrl(string sso_method)
    {
        var ssoModuleConfig = _configuration.GetSection("OAuth:" + ToPascalCase(sso_method));
        var clientId = ssoModuleConfig["ClientId"];
        var authority = ssoModuleConfig["Authority"];
        var redirectUri = ssoModuleConfig["RedirectUri"];
        var scope = ssoModuleConfig["Scope"];
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(authority) || 
            string.IsNullOrEmpty(redirectUri) || string.IsNullOrEmpty(scope))
        {
            throw new ArgumentException("SSO method configuration is invalid");
        }
        var state = GenerateSecureRandomString(32);
        _stateStore[state] = DateTime.UtcNow.AddMinutes(10);
        CleanExpiredStates();
        var queryParams = HttpUtility.ParseQueryString(string.Empty);
        queryParams["response_type"] = "code";
        queryParams["client_id"] = clientId;
        queryParams["redirect_uri"] = redirectUri;
        queryParams["scope"] = scope;
        queryParams["state"] = state;
        var authUrl = $"{authority}?{queryParams}";
        return new SsoUrlResponse
        {
            auth_url = authUrl,
            state = state
        };
    }

    public async Task<LoginResponse> LoginWithSSO(string sso_method, SsoLoginRequest request)
    {
        if (!_stateStore.TryGetValue(request.state, out DateTime value) || value < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired state parameter");
        }
        _stateStore.Remove(request.state);
        var ssoModuleConfig = _configuration.GetSection("OAuth:" + ToPascalCase(sso_method));
        var clientId = ssoModuleConfig["ClientId"];
        var clientSecret = ssoModuleConfig["ClientSecret"];
        var authority = ssoModuleConfig["Authority"];
        var redirectUri = ssoModuleConfig["RedirectUri"];
        var groupMappingSection = ssoModuleConfig.GetSection("GroupMapping");
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || 
            string.IsNullOrEmpty(authority) || string.IsNullOrEmpty(redirectUri))
        {
            throw new ArgumentException("SSO method configuration is invalid");
        }
        var tokenResponse = await ExchangeCodeForToken(request.code, clientId!, clientSecret!, authority!, redirectUri!);
        var userInfo = await GetUserInfo(tokenResponse.access_token, authority!);
        _logger.LogDebug("SSO user info received: {UserInfo}", JsonSerializer.Serialize(userInfo));
        var (user, externalLoginId) = await GetOrCreateUser(sso_method, userInfo, groupMappingSection);
        var jwt = await _jwtService.GenerateToken(user, "sso_" + sso_method);
        await _jwiService.SaveToken(jwt, user.id_user, "sso_" + sso_method, externalLoginId: externalLoginId);
        try
        {
            var notification = new NotificationMessage
            {
                RecipientEmail = user.email_user,
                TemplateId = "login-detected",
                Language = _configuration.GetAppLanguage()
            };
            await _kafkaProducerService.PublishAsync(
                KafkaNotificationTopic,
                user.email_user + "-sso-login",
                JsonSerializer.Serialize(notification)
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to send login notification email to {Email}", user.email_user);
        }
        return new LoginResponse
        {
            token = jwt.token,
            expire_date_token = jwt.expire_date_token,
            refresh_token = jwt.refresh_token,
            expire_date_refresh_token = jwt.expire_date_refresh_token,
            user = user
        };
    }

    private async Task<TokenResponse> ExchangeCodeForToken(string code, string clientId, string clientSecret, string authority, string redirectUri)
    {
        var tokenEndpoint = authority.Replace("/authorize/", "/token/");
        var requestBody = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "authorization_code"),
            new("code", code),
            new("redirect_uri", redirectUri),
            new("client_id", clientId),
            new("client_secret", clientSecret)
        };
        var content = new FormUrlEncodedContent(requestBody);
        var httpClient = new HttpClient();
        var response = await httpClient.PostAsync(tokenEndpoint, content);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Error exchanging code for token: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
            throw new HttpRequestException($"Error exchanging code for token: {response.StatusCode}");
        }
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(jsonResponse, JsonOptions);
        return tokenResponse ?? throw new InvalidOperationException("Invalid token response");
    }

    private async Task<UserInfoResponse> GetUserInfo(string accessToken, string authority)
    {
        var userInfoEndpoint = authority.Replace("/application/o/authorize/", "/application/o/userinfo/");
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var response = await httpClient.GetAsync(userInfoEndpoint);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Error retrieving user info: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
            throw new HttpRequestException($"Error retrieving user info: {response.StatusCode}");
        }
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var userInfo = JsonSerializer.Deserialize<UserInfoResponse>(jsonResponse, JsonOptions);
        return userInfo ?? throw new InvalidOperationException("Invalid user info response");
    }

    private async Task<(ReadUserDto user, int externalLoginId)> GetOrCreateUser(string provider, UserInfoResponse userInfo, IConfigurationSection groupMappingSection = null!)
    {
        if (string.IsNullOrEmpty(userInfo.Sub))
        {
            throw new InvalidOperationException("Invalid user info response");
        }
        var userRole = Enums.UserRole.User;
        if (groupMappingSection != null)
        {
            foreach (var role in Enum.GetValues<Enums.UserRole>())
            {
                var mappedGroup = groupMappingSection[role.ToString()];
                if ((!string.IsNullOrEmpty(mappedGroup)) && userInfo.Groups.Contains(mappedGroup) && (role > userRole)) // Assign the highest role found
                {
                    userRole = role;
                }
            }
        }
        var externalLogin = await _context.UsersExternalLogins
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.provider == provider && l.provider_subject == userInfo.Sub);
        var existingUser = externalLogin?.User ?? await _context.Users.FirstOrDefaultAsync(u => u.email_user == userInfo.Email);
        if (existingUser != null)
        {
            if (!existingUser.is_active)
            {
                throw new UnauthorizedAccessException("Account is disabled");
            }
            if (externalLogin is null)
            {
                // first SSO login of an existing account (matched by email): link it to the identity provider
                externalLogin = new UsersExternalLogins
                {
                    id_user = existingUser.id_user,
                    provider = provider,
                    provider_subject = userInfo.Sub
                };
                _context.UsersExternalLogins.Add(externalLogin);
            }
            if ((groupMappingSection != null) && (existingUser.role_user != userRole))
            {
                // Update user role if it has changed
                existingUser.role_user = userRole;
            }
            existingUser.last_login = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return (_mapper.Map<ReadUserDto>(existingUser), externalLogin.id_user_external_login);
        }
        return await _userService.CreateExternalUser(
            string.IsNullOrWhiteSpace(userInfo.FamilyName) ? "SSO" : userInfo.FamilyName,
            string.IsNullOrWhiteSpace(userInfo.GivenName) ? "User" : userInfo.GivenName,
            userInfo.Email,
            userRole,
            provider,
            userInfo.Sub);
    }

    public async Task<bool> CheckUserPasswordByEmail(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == email) ?? throw new KeyNotFoundException($"User with email '{email}' not found");
        var userPassword = await _context.UsersPasswords.FindAsync(user.id_user);
        return userPassword.IsPasswordValid(password);
    }

    public async Task<bool> CheckUserPasswordById(int id, string password)
    {
        var userPassword = await _context.UsersPasswords.FindAsync(id);
        return userPassword.IsPasswordValid(password);
    }

    public async Task ForgotPassword(ForgotPasswordRequest request)
    {
        //check if SMTP is Enabled
        if (!_configuration.IsSmtpEnabled())
        {
            throw new InvalidOperationException("SMTP is not enabled");
        }
        // check if user exists
        var userPassword = await _context.UsersPasswords
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.User!.email_user == request.email);
        if (userPassword is null)
        {
            // a user with an external login only has no password yet: an empty one blocks the password login until the reset is done
            var externalUser = await _context.Users.FirstOrDefaultAsync(u => u.email_user == request.email);
            if (externalUser is not null)
            {
                userPassword = new UsersPasswords { User = externalUser, password_user = string.Empty };
                _context.UsersPasswords.Add(userPassword);
            }
        }
        // a reset email has just been sent: do nothing (and answer the same way, so the caller can't tell if the email exists)
        if (userPassword?.User is not null && !(userPassword.reset_token_sent_at > DateTime.UtcNow - AuthDelays.EmailResendCooldown))
        {
            var user = userPassword.User;
            // add reset_token
            userPassword.reset_token = Guid.NewGuid();
            userPassword.reset_token_expiration = DateTime.Now.AddHours(1);
            userPassword.reset_token_sent_at = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            // send email with reset_token
            try
            {
                var notification = new NotificationMessage
                {
                        RecipientEmail = user.email_user,
                    TemplateId = "password-reset",
                    Language = _configuration.GetAppLanguage(),
                    TemplateValues = new Dictionary<string, string>
                    {
                        ["resetLink"] = _configuration["FrontendUrl"] + "/reset-password?token=" + userPassword.reset_token.ToString() + "&email=" + user.email_user
                    }
                };
                await _kafkaProducerService.PublishAsync(
                    KafkaNotificationTopic,
                    user.email_user + "-password-reset",
                    JsonSerializer.Serialize(notification)
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to send password reset email to {Email}", user.email_user);
            }
        }
    }

    public async Task ResetPassword(ResetPasswordRequest request)
    {
        //check if SMTP is Enabled
        if (!_configuration.IsSmtpEnabled())
        {
            throw new InvalidOperationException("SMTP is not enabled");
        }
        // check if token is valid
        var userPassword = await _context.UsersPasswords
            .Include(p => p.User)
            .FirstOrDefaultAsync(
                p => p.User!.email_user == request.email && p.reset_token.ToString() == request.token && p.reset_token_expiration > DateTime.Now
            ) ?? throw new InvalidOperationException("Invalid token");
        var user = userPassword.User!;
        // update password
        userPassword.password_user = BCrypt.Net.BCrypt.HashPassword(request.password);
        userPassword.password_last_changed = DateTime.UtcNow;
        userPassword.reset_token = null;
        userPassword.reset_token_expiration = null;
        await _context.SaveChangesAsync();
        await _jwiService.RevokeAllAccessTokenByUser(user.id_user, "User reset password");
        await _jwiService.RevokeAllRefreshTokenByUser(user.id_user, "User reset password");
        // send email to the user
        try
        {
            var notification = new NotificationMessage
            {
                RecipientEmail = user.email_user,
                TemplateId = "password-changed",
                Language = _configuration.GetAppLanguage()
            };
            await _kafkaProducerService.PublishAsync(
                KafkaNotificationTopic,
                user.email_user + "-password-changed",
                JsonSerializer.Serialize(notification)
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to send password changed notification email to {Email}", user.email_user);
        }
    }

    public async Task ResendVerification(ResendVerificationRequest request)
    {
        if (!_configuration.IsSmtpEnabled())
        {
            throw new InvalidOperationException("SMTP is not enabled");
        }
        // nothing is returned about the email (unknown, already verified or too soon after the previous one)
        await _userService.SendEmailVerification(request.email);
    }

    public async Task VerifyEmail(VerifyEmailRequest request)
    {
        if (!Guid.TryParse(request.token, out var token))
        {
            throw new InvalidOperationException("Invalid token");
        }
        var user = await _context.Users.FirstOrDefaultAsync(
            u => u.email_user == request.email && u.email_verification_token == token && u.email_verification_token_expiration > DateTime.UtcNow
        ) ?? throw new InvalidOperationException("Invalid token");
        user.is_verified = true;
        user.email_verification_token = null;
        user.email_verification_token_expiration = null;
        await _context.SaveChangesAsync();
    }

    public async Task<LoginResponse> LoginWithPassword(LoginRequest request)
    {
        // check if user exists
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == request.email) ?? throw new UnauthorizedAccessException("Invalid password"); // do not reveal if email exists
        // check if password is correct
        var userPassword = await _context.UsersPasswords.FindAsync(user.id_user);
        if (!userPassword.IsPasswordValid(request.password))
        {
            throw new UnauthorizedAccessException("Invalid password");
        }
        if (!user.is_active)
        {
            throw new UnauthorizedAccessException("Account is disabled");
        }
        user.last_login = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        // generate tokens
        var token = await _jwtService.GenerateToken(_mapper.Map<ReadUserDto>(user), "user_password");
        await _jwiService.SaveToken(token, user.id_user, "user_password");
        // send email to the user
        try
        {
            var notification = new NotificationMessage
            {
                RecipientEmail = user.email_user,
                TemplateId = "login-detected",
                Language = _configuration.GetAppLanguage()
            };
            await _kafkaProducerService.PublishAsync(
                KafkaNotificationTopic,
                user.email_user + "-login-detected",
                JsonSerializer.Serialize(notification)
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to send login notification email to {Email}", user.email_user);
        }
        // return tokens
        return new LoginResponse
        {
            token = token.token,
            expire_date_token = token.expire_date_token,
            refresh_token = token.refresh_token,
            expire_date_refresh_token = token.expire_date_refresh_token,
            user = _mapper.Map<ReadUserDto>(user)
        };
    }

    public async Task<LoginResponse> RefreshJwt()
    {
        var clientId = _sessionService.GetClientId();
        var tokenId = _sessionService.GetTokenId();
        var authMethod = _sessionService.GetTokenAuthMethod();
        var sessionId = await _jwiService.GetSessionIdByTokenId(tokenId, clientId);
        var user = await _context.Users.FindAsync(clientId) ?? throw new KeyNotFoundException($"User with id '{clientId}' not found");
        if (!user.is_active)
        {
            throw new UnauthorizedAccessException("Account is disabled");
        }
        user.last_login = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        var token = await _jwtService.GenerateToken(_mapper.Map<ReadUserDto>(user), authMethod);
        await _jwiService.RevokePairTokenByRefreshToken(tokenId, "User refresh token", clientId);
        await _jwiService.SaveToken(token, user.id_user, authMethod, sessionId);
        // return tokens
        return new LoginResponse
        {
            token = token.token,
            expire_date_token = token.expire_date_token,
            refresh_token = token.refresh_token,
            expire_date_refresh_token = token.expire_date_refresh_token,
            user = _mapper.Map<ReadUserDto>(user)
        };
    }

    /* public async Task Logout()
    {
        var clientId = _sessionService.GetClientId();
        var tokenId = _sessionService.GetTokenId();
        await _jwiService.RevokeAllAccessTokenByUser(clientId, "User logout");
        await _jwiService.RevokeAllRefreshTokenByUser(clientId, "User logout");
    } */

    private static string GenerateSecureRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ@!:;,?.-abcdefghijklmnopqrstuvwxyz0123456789";
        var random = new char[length];
        
        using (var rng = RandomNumberGenerator.Create())
        {
            var bytes = new byte[length];
            rng.GetBytes(bytes);
            
            for (int i = 0; i < length; i++)
            {
                random[i] = chars[bytes[i] % chars.Length];
            }
        }
        
        return new string(random);
    }

    private static void CleanExpiredStates()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _stateStore.Where(kvp => kvp.Value < now).Select(kvp => kvp.Key).ToList();
        foreach (var key in expiredKeys)
        {
            _stateStore.Remove(key);
        }
    }

    private static string ToPascalCase(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var words = text.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
        }
        return string.Join(string.Empty, words);
    }

    private sealed class TokenResponse
    {
        public string access_token { get; set; } = string.Empty;
        public string token_type { get; set; } = string.Empty;
        public string id_token { get; set; } = string.Empty;
        public string scope { get; set; } = string.Empty;
    }

    private sealed class UserInfoResponse
    {
        public string Sub { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        [JsonPropertyName("given_name")]
        public string GivenName { get; set; } = string.Empty;
        [JsonPropertyName("family_name")]
        public string FamilyName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("preferred_username")]
        public string PreferredUsername { get; set; } = string.Empty;
        public string[] Groups { get; set; } = [];
    }
}