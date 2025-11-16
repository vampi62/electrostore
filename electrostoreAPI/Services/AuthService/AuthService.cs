using AutoMapper;
using System.Text.Json;
using electrostore.Dto;
using electrostore.Services.SmtpService;
using electrostore.Services.SessionService;
using electrostore.Services.UserService;
using electrostore.Services.JwiService;
using System.Security.Cryptography;
using System.Web;
using Microsoft.EntityFrameworkCore;

namespace electrostore.Services.AuthService;

public class AuthService : IAuthService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ISmtpService _smtpService;
    private readonly ISessionService _sessionService;
    private readonly IUserService _userService;
    private readonly JwtService.JwtService _jwtService;
    private readonly IJwiService _jwiService;
    private static readonly Dictionary<string, DateTime> _stateStore = new();
    // In-memory store for state parameters, if you want persistence or use duplication across instances, consider using a distributed cache like Redis

    public AuthService(IMapper mapper, ApplicationDbContext context, IConfiguration configuration, ISmtpService smtpService, ISessionService sessionService, IUserService userService, JwtService.JwtService jwtService, IJwiService jwiService)
    {
        _mapper = mapper;
        _context = context;
        _configuration = configuration;
        _smtpService = smtpService;
        _sessionService = sessionService;
        _userService = userService;
        _jwtService = jwtService;
        _jwiService = jwiService;
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
            AuthUrl = authUrl,
            State = state
        };
    }

    public async Task<LoginResponse> LoginWithSSO(string sso_method, SsoLoginRequest request)
    {
        if (!_stateStore.TryGetValue(request.State, out DateTime value) || value < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired state parameter");
        }
        _stateStore.Remove(request.State);
        var ssoModuleConfig = _configuration.GetSection("OAuth:" + ToPascalCase(sso_method));
        var clientId = ssoModuleConfig["ClientId"];
        var clientSecret = ssoModuleConfig["ClientSecret"];
        var authority = ssoModuleConfig["Authority"];
        var redirectUri = ssoModuleConfig["RedirectUri"];
        var tokenResponse = await ExchangeCodeForToken(request.Code, clientId!, clientSecret!, authority!, redirectUri!);
        var userInfo = await GetUserInfo(tokenResponse.access_token, authority!);
        var user = await GetOrCreateUser(userInfo);
        var jwt = _jwtService.GenerateToken(user, "sso_" + sso_method);
        await _jwiService.SaveToken(jwt, user.id_user, "sso_" + sso_method);
        await _smtpService.SendEmailAsync(
            user.email_user,
            "Login",
            "A new login has been detected on your account. If this was not you, please change your password."
        );
        return new LoginResponse
        {
            token = jwt.token,
            expire_date_token = jwt.expire_date_token,
            refresh_token = jwt.refresh_token,
            expire_date_refresh_token = jwt.expire_date_refresh_token,
            user = user
        };
    }

    private static async Task<TokenResponse> ExchangeCodeForToken(string code, string clientId, string clientSecret, string authority, string redirectUri)
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
            Console.WriteLine(errorContent);
            throw new HttpRequestException($"Error exchanging code for token: {response.StatusCode}");
        }
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return tokenResponse ?? throw new InvalidOperationException("Invalid token response");
    }

    private static async Task<UserInfoResponse> GetUserInfo(string accessToken, string authority)
    {
        var userInfoEndpoint = authority.Replace("/application/o/authorize/", "/application/o/userinfo/");
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var response = await httpClient.GetAsync(userInfoEndpoint);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine(errorContent);
            throw new HttpRequestException($"Error retrieving user info: {response.StatusCode}");
        }
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var userInfo = JsonSerializer.Deserialize<UserInfoResponse>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return userInfo ?? throw new InvalidOperationException("Invalid user info response");
    }

    private async Task<ReadUserDto> GetOrCreateUser(UserInfoResponse userInfo)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.email_user == userInfo.Email);
        if (existingUser != null)
        {
            return _mapper.Map<ReadUserDto>(existingUser);
        }
        var newUserDto = new CreateUserDto
        {
            nom_user = userInfo.FamilyName ?? "SSO",
            prenom_user = userInfo.GivenName ?? "User",
            email_user = userInfo.Email,
            mdp_user = GenerateSecureRandomString(32),
            role_user = Enums.UserRole.User
        };
        return await _userService.CreateUser(newUserDto);
    }

    public async Task<bool> CheckUserPasswordByEmail(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == email) ?? throw new KeyNotFoundException($"User with email '{email}' not found");
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
            await _smtpService.SendEmailAsync(
                request.Email,
                "Reset password",
                "Click on the following link to reset your password: " + _configuration["FrontendUrl"] + "/reset-password?token=" + user.reset_token.ToString() + "&email=" + user.email_user
            );
        }
    }

    public async Task ResetPassword(ResetPasswordRequest request)
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
        await _jwiService.RevokeAllAccessTokenByUser(user.id_user, "User reset password");
        await _jwiService.RevokeAllRefreshTokenByUser(user.id_user, "User reset password");
        // send email to the user
        await _smtpService.SendEmailAsync(
            user.email_user,
            "Password changed",
            "Your password has been changed"
        );
    }

    public async Task<LoginResponse> LoginWithPassword(LoginRequest request)
    {
        // check if user exists
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email_user == request.Email) ?? throw new KeyNotFoundException($"User with email '{request.Email}' not found");
        // check if password is correct
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.mdp_user))
        {
            throw new UnauthorizedAccessException("Invalid password");
        }
        // generate tokens
        var token = _jwtService.GenerateToken(_mapper.Map<ReadUserDto>(user), "user_password");
        await _jwiService.SaveToken(token, user.id_user, "user_password");
        // send email to the user
        await _smtpService.SendEmailAsync(
            user.email_user,
            "Login",
            "A new login has been detected on your account. If this was not you, please change your password."
        );
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
        var token = _jwtService.GenerateToken(_mapper.Map<ReadUserDto>(user), authMethod);
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

        var words = text.Split(new char[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
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
    }
}