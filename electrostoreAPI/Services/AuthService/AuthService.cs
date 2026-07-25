using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Kafka.Messages;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Services.UserService;
using ElectrostoreAPI.Services.JwiService;
using ElectrostoreAPI.Services.JwtService;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.Json;
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
            _logger.LogWarning("GetSSOAuthUrl: SSO method {SsoMethod} configuration is invalid", sso_method);
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
        _logger.LogInformation("GetSSOAuthUrl: generated SSO auth url for method {SsoMethod}", sso_method);
        return new SsoUrlResponse
        {
            AuthUrl = authUrl,
            State = state
        };
    }

    public async Task<LoginResponse> LoginWithSSO(string sso_method, SsoLoginRequest request)
    {
        if (!_stateStore.TryGetValue(request.State, out DateTime value) || value < DateTime.UtcNow)
        {
            _logger.LogWarning("LoginWithSSO: invalid or expired state parameter for method {SsoMethod}", sso_method);
            throw new UnauthorizedAccessException("Invalid or expired state parameter");
        }
        _stateStore.Remove(request.State);
        var ssoModuleConfig = _configuration.GetSection("OAuth:" + ToPascalCase(sso_method));
        var clientId = ssoModuleConfig["ClientId"];
        var clientSecret = ssoModuleConfig["ClientSecret"];
        var authority = ssoModuleConfig["Authority"];
        var redirectUri = ssoModuleConfig["RedirectUri"];
        var groupMappingSection = ssoModuleConfig.GetSection("GroupMapping");
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) ||
            string.IsNullOrEmpty(authority) || string.IsNullOrEmpty(redirectUri))
        {
            _logger.LogWarning("LoginWithSSO: SSO method {SsoMethod} configuration is invalid", sso_method);
            throw new ArgumentException("SSO method configuration is invalid");
        }
        var tokenResponse = await ExchangeCodeForToken(request.Code, clientId!, clientSecret!, authority!, redirectUri!);
        var userInfo = await GetUserInfo(tokenResponse.access_token, authority!);
        _logger.LogDebug("LoginWithSSO: retrieved user info for email {Email} via method {SsoMethod}", userInfo.Email, sso_method);
        var user = await GetOrCreateUser(userInfo, groupMappingSection);
        var jwt = await _jwtService.GenerateToken(user, "sso_" + sso_method);
        await _jwiService.SaveToken(jwt, user.id_user, "sso_" + sso_method);
        try
        {
            var notification = new NotificationMessage
            {
                Types = ["email"],
                RecipientEmail = user.email_user,
                TemplateId = "login-detected",
                Language = _configuration.GetValue<string>("AppLanguage") ?? "fr"
            };
            await _kafkaProducerService.PublishAsync(
                "notification-requests",
                user.email_user + "-sso-login",
                JsonSerializer.Serialize(notification)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LoginWithSSO: unable to send login notification email for user {UserId}", user.id_user);
        }
        _logger.LogInformation("LoginWithSSO: user {UserId} logged in via {SsoMethod}", user.id_user, sso_method);
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
            _logger.LogError("ExchangeCodeForToken: failed to exchange code for token, status {StatusCode}", response.StatusCode);
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
            _logger.LogError("GetUserInfo: failed to retrieve user info, status {StatusCode}", response.StatusCode);
            throw new HttpRequestException($"Error retrieving user info: {response.StatusCode}");
        }
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var userInfo = JsonSerializer.Deserialize<UserInfoResponse>(jsonResponse, JsonOptions);
        return userInfo ?? throw new InvalidOperationException("Invalid user info response");
    }

    private async Task<ReadUserDto> GetOrCreateUser(UserInfoResponse userInfo, IConfigurationSection groupMappingSection = null!)
    {
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
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.email_user == userInfo.Email);
        if (existingUser != null)
        {
            if ((groupMappingSection != null) && (existingUser.role_user != userRole))
            {
                // Update user role if it has changed
                existingUser.role_user = userRole;
                await _context.SaveChangesAsync();
                _logger.LogInformation("GetOrCreateUser: updated role for user {UserId} to {UserRole}", existingUser.id_user, userRole);
            }
            return _mapper.Map<ReadUserDto>(existingUser);
        }
        var newUserDto = new CreateUserDto
        {
            nom_user = userInfo.FamilyName ?? "SSO",
            prenom_user = userInfo.GivenName ?? "User",
            email_user = userInfo.Email,
            mdp_user = GenerateSecureRandomString(32),
            role_user = userRole
        };
        return await _userService.CreateUser(newUserDto, true); // true indicates that this is an SSO user login, so we avoid role checks
    }

    public async Task<bool> CheckUserPasswordByEmail(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == email);
        if (user is null)
        {
            _logger.LogWarning("CheckUserPasswordByEmail: user with email {Email} not found", email);
            throw new KeyNotFoundException($"User with email '{email}' not found");
        }
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
        if (bool.TryParse(_configuration["SMTP:Enable"], out var isEnabled) && !isEnabled)
        {
            _logger.LogWarning("ForgotPassword: SMTP is not enabled");
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
            _logger.LogInformation("ForgotPassword: reset token generated for user {UserId}", user.id_user);
            // send email with reset_token
            try
            {
                var notification = new NotificationMessage
                {
                    Types = ["email"],
                    RecipientEmail = user.email_user,
                    TemplateId = "password-reset",
                    Language = _configuration.GetValue<string>("AppLanguage") ?? "fr",
                    TemplateValues = new Dictionary<string, string>
                    {
                        ["resetLink"] = _configuration["FrontendUrl"] + "/reset-password?token=" + user.reset_token.ToString() + "&email=" + user.email_user
                    }
                };
                await _kafkaProducerService.PublishAsync(
                    "notification-requests",
                    user.email_user + "-password-reset",
                    JsonSerializer.Serialize(notification)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ForgotPassword: unable to send password reset email for user {UserId}", user.id_user);
            }
        }
    }

    public async Task ResetPassword(ResetPasswordRequest request)
    {
        //check if SMTP is Enabled
        if (bool.TryParse(_configuration["SMTP:Enable"], out var isEnabled) && !isEnabled)
        {
            _logger.LogWarning("ResetPassword: SMTP is not enabled");
            throw new InvalidOperationException("SMTP is not enabled");
        }
        // check if token is valid
        var user = await _context.Users.FirstOrDefaultAsync(
            u => u.email_user == request.Email && u.reset_token.ToString() == request.Token && u.reset_token_expiration > DateTime.Now
        );
        if (user is null)
        {
            _logger.LogWarning("ResetPassword: invalid or expired reset token for email {Email}", request.Email);
            throw new InvalidOperationException("Invalid token");
        }
        // update password
        user.mdp_user = BCrypt.Net.BCrypt.HashPassword(request.Password);
        user.reset_token = null;
        user.reset_token_expiration = null;
        await _context.SaveChangesAsync();
        await _jwiService.RevokeAllAccessTokenByUser(user.id_user, "User reset password");
        await _jwiService.RevokeAllRefreshTokenByUser(user.id_user, "User reset password");
        _logger.LogInformation("ResetPassword: password reset for user {UserId}", user.id_user);
        // send email to the user
        try
        {
            var notification = new NotificationMessage
            {
                Types = ["email"],
                RecipientEmail = user.email_user,
                TemplateId = "password-changed",
                Language = _configuration.GetValue<string>("AppLanguage") ?? "fr"
            };
            await _kafkaProducerService.PublishAsync(
                "notification-requests",
                user.email_user + "-password-changed",
                JsonSerializer.Serialize(notification)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ResetPassword: unable to send password changed notification email for user {UserId}", user.id_user);
        }
    }

    public async Task<LoginResponse> LoginWithPassword(LoginRequest request)
    {
        // check if user exists
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == request.Email);
        if (user is null)
        {
            _logger.LogWarning("LoginWithPassword: no user found for email {Email}", request.Email); // do not reveal if email exists
            throw new UnauthorizedAccessException("Invalid password"); // do not reveal if email exists
        }
        // check if password is correct
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.mdp_user))
        {
            _logger.LogWarning("LoginWithPassword: invalid password for user {UserId}", user.id_user);
            throw new UnauthorizedAccessException("Invalid password");
        }
        // generate tokens
        var token = await _jwtService.GenerateToken(_mapper.Map<ReadUserDto>(user), "user_password");
        await _jwiService.SaveToken(token, user.id_user, "user_password");
        // send email to the user
        try
        {
            var notification = new NotificationMessage
            {
                Types = ["email"],
                RecipientEmail = user.email_user,
                TemplateId = "login-detected",
                Language = _configuration.GetValue<string>("AppLanguage") ?? "fr"
            };
            await _kafkaProducerService.PublishAsync(
                "notification-requests",
                user.email_user + "-login-detected",
                JsonSerializer.Serialize(notification)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LoginWithPassword: unable to send login notification email for user {UserId}", user.id_user);
        }
        _logger.LogInformation("LoginWithPassword: user {UserId} logged in", user.id_user);
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
        var user = await _context.Users.FindAsync(clientId);
        if (user is null)
        {
            _logger.LogWarning("RefreshJwt: user {UserId} not found", clientId);
            throw new KeyNotFoundException($"User with id '{clientId}' not found");
        }
        var token = await _jwtService.GenerateToken(_mapper.Map<ReadUserDto>(user), authMethod);
        await _jwiService.RevokePairTokenByRefreshToken(tokenId, "User refresh token", clientId);
        await _jwiService.SaveToken(token, user.id_user, authMethod, sessionId);
        _logger.LogInformation("RefreshJwt: token refreshed for user {UserId}", user.id_user);
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
        public string GivenName { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PreferredUsername { get; set; } = string.Empty;
        public string[] Groups { get; set; } = [];
    }
}