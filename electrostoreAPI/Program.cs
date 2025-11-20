using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Minio;

using MQTTnet;
using electrostore.Dto;
using electrostore.Enums;

using electrostore.Services.AuthService;
using electrostore.Services.BoxService;
using electrostore.Services.BoxTagService;
using electrostore.Services.CameraService;
using electrostore.Services.CommandCommentaireService;
using electrostore.Services.CommandDocumentService;
using electrostore.Services.CommandItemService;
using electrostore.Services.CommandService;
using electrostore.Services.ConfigService;
using electrostore.Services.FileService;
using electrostore.Services.IAService;
using electrostore.Services.ImgService;
using electrostore.Services.ItemBoxService;
using electrostore.Services.ItemDocumentService;
using electrostore.Services.ItemService;
using electrostore.Services.ItemTagService;
using electrostore.Services.JwiService;
using electrostore.Services.LedService;
using electrostore.Services.ProjetCommentaireService;
using electrostore.Services.ProjetDocumentService;
using electrostore.Services.ProjetItemService;
using electrostore.Services.ProjetProjetTagService;
using electrostore.Services.ProjetService;
using electrostore.Services.ProjetStatusService;
using electrostore.Services.ProjetTagService;
using electrostore.Services.SessionService;
using electrostore.Services.SmtpService;
using electrostore.Services.StoreService;
using electrostore.Services.StoreTagService;
using electrostore.Services.TagService;
using electrostore.Services.UserService;
using electrostore.Services.ValidateStoreService;
using electrostore.Services.JwtService;
using electrostore.Middleware;

using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;

namespace electrostore;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true);
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile("config/appsettings.Development.json", optional: true, reloadOnChange: true);
        }

        var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings
        {
            Key = "default_key_value",
            Issuer = "default_issuer_value",
            Audience = "default_audience_value"
        };
        var key = Encoding.ASCII.GetBytes(jwtSettings.Key);

        AddScopes(builder);

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddControllers(options => { options.Filters.Add(new AuthorizeFilter()); })
            // Invalid model state response factory
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var validationErrors = context.ModelState
                        .Where(ms => ms.Value != null && ms.Value.Errors.Count > 0)
                        .Select(kvp => new
                        {
                            Field = kvp.Key,
                            Errors = kvp.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message ?? "Invalid value" : e.ErrorMessage).ToArray()
                        })
                        .ToList();
                    // if first error contain "JSON deserialization"
                    // this issue comes from a bad JSON format in the request body
                    // so we search in the error the missing field (found after ":" and separate by ";") and return a specific message with it
                    if (validationErrors.Count > 0 && validationErrors[0].Errors.Any(e => e.Contains("JSON deserialization", StringComparison.OrdinalIgnoreCase)))
                    {
                        var missingsField = validationErrors[0].Errors
                            .Where(e => e.Contains("JSON deserialization", StringComparison.OrdinalIgnoreCase))
                            .SelectMany(e =>
                            {
                                var parts = e.Split(':', ';');
                                if (parts.Length > 1)
                                {
                                    return parts.Skip(1).Select(p => p.Trim());
                                }
                                return Array.Empty<string>();
                            })
                            .ToList();
                        var errorMessage = "Malformed JSON request body.";
                        var ex = new { error = errorMessage, details = $"Please check the format of the following field(s): {string.Join(", ", missingsField)}" };
                        return new BadRequestObjectResult(ex);
                    }
                    else if (validationErrors.Count != 0)
                    {
                        var ex = new { error = "Validation Failed", details = validationErrors };
                        return new BadRequestObjectResult(ex);
                    }

                    return new BadRequestResult();
                };
            });

        builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Program).Assembly));
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "ElectroStore API", Version = "v1" });
            // api can return in the header "X-Total-Count" the total number of items
            c.OperationFilter<AddTotalCountHeaderFilter>();
        });

        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

        builder.Logging.AddFilter("LuckyPennySoftware.AutoMapper.License", LogLevel.None);

        AddAuthentication(builder, key);

        var app = builder.Build();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseStaticFiles();

        // if S3 is not enabled create the required directories
        if (!builder.Configuration.GetSection("S3:Enable").Get<bool>())
        {
            CreateRequiredDirectories();
        }

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });
        app.UseCors("CorsPolicy");

        app.UseAuthentication();

        app.UseAuthorization();

        app.UseMiddleware<ExceptionsHandler>();

        app.MapControllers();

        InitializeDatabase(app);

        app.Run();
    }

    private static void AddAuthentication(WebApplicationBuilder builder, byte[] key)
    {
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var jwiService = context.HttpContext.RequestServices.GetRequiredService<IJwiService>();
                        if (context.SecurityToken is not JsonWebToken token)
                        {
                            context.Fail("Token is invalid");
                        }
                        else
                        {
                            var roleClaim = token.Claims.FirstOrDefault(x => x.Type == "role" && (x.Value == "access" || x.Value == "refresh"));
                            if (roleClaim is null)
                            {
                                context.Fail("Token is invalid");
                            }
                            else if (jwiService.IsRevoked(token.Id, roleClaim.Value))
                            {
                                context.Fail("Token is revoked");
                            }
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("RefreshToken", policy =>
                policy.RequireRole("refresh"))
            .AddPolicy("AccessToken", policy =>
                policy.RequireRole("access"));

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy",
                cors =>
                {
                    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                    cors.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });
    }

    private static void AddScopes(WebApplicationBuilder builder)
    {
        // Add services to the container.
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), new MySqlServerVersion(new Version(11, 4, 7))));

        builder.Services.AddSingleton<IMqttClient>(sp =>
        {
            var factory = new MqttClientFactory();
            var mqttClient = factory.CreateMqttClient();
            var options = new MqttClientOptionsBuilder()
                .WithClientId(builder.Configuration.GetSection("MQTT:ClientId").Value)
                .WithTcpServer(builder.Configuration.GetSection("MQTT:Server").Value, builder.Configuration.GetSection("MQTT:Port").Get<int>())
                .WithCredentials(builder.Configuration.GetSection("MQTT:Username").Value, builder.Configuration.GetSection("MQTT:Password").Value)
                .WithCleanSession()
                .Build();
            mqttClient.ConnectAsync(options);
            return mqttClient;
        });
        builder.Services.AddSingleton<IMinioClient>(sp =>
        {
            var minioClient = new MinioClient()
                .WithEndpoint(builder.Configuration.GetSection("S3:Endpoint").Value ?? "localhost:9000")
                .WithCredentials(
                    builder.Configuration.GetSection("S3:AccessKey").Value ?? "minioadmin",
                    builder.Configuration.GetSection("S3:SecretKey").Value ?? "minioadmin")
                .WithRegion(builder.Configuration.GetSection("S3:Region").Value ?? "us-east-1")
                .WithSSL(builder.Configuration.GetSection("S3:Secure").Get<bool>())
                .Build();
            return minioClient;
        });

        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IBoxService, BoxService>();
        builder.Services.AddScoped<IBoxTagService, BoxTagService>();
        builder.Services.AddScoped<ICameraService, CameraService>();
        builder.Services.AddScoped<ICommandCommentaireService, CommandCommentaireService>();
        builder.Services.AddScoped<ICommandDocumentService, CommandDocumentService>();
        builder.Services.AddScoped<ICommandItemService, CommandItemService>();
        builder.Services.AddScoped<ICommandService, CommandService>();
        builder.Services.AddScoped<IConfigService, ConfigService>();
        builder.Services.AddScoped<IFileService, FileService>();
        builder.Services.AddScoped<IIAService, IAService>();
        builder.Services.AddScoped<IImgService, ImgService>();
        builder.Services.AddScoped<IItemBoxService, ItemBoxService>();
        builder.Services.AddScoped<IItemDocumentService, ItemDocumentService>();
        builder.Services.AddScoped<IItemService, ItemService>();
        builder.Services.AddScoped<IItemTagService, ItemTagService>();
        builder.Services.AddScoped<ILedService, LedService>();
        builder.Services.AddScoped<IProjetCommentaireService, ProjetCommentaireService>();
        builder.Services.AddScoped<IProjetDocumentService, ProjetDocumentService>();
        builder.Services.AddScoped<IProjetItemService, ProjetItemService>();
        builder.Services.AddScoped<IProjetProjetTagService, ProjetProjetTagService>();
        builder.Services.AddScoped<IProjetService, ProjetService>();
        builder.Services.AddScoped<IProjetStatusService, ProjetStatusService>();
        builder.Services.AddScoped<IProjetTagService, ProjetTagService>();
        builder.Services.AddScoped<ISessionService, SessionService>();
        builder.Services.AddScoped<ISmtpService, SmtpService>();
        builder.Services.AddScoped<IStoreService, StoreService>();
        builder.Services.AddScoped<IStoreTagService, StoreTagService>();
        builder.Services.AddScoped<ITagService, TagService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IValidateStoreService, ValidateStoreService>();
        builder.Services.AddScoped<IJwiService, JwiService>();
        builder.Services.AddSingleton<JwtService>();
    }

    private static void CreateRequiredDirectories()
    {
        if (!Directory.Exists("wwwroot/images"))
        {
            Directory.CreateDirectory("wwwroot/images");
        }
        if (!Directory.Exists("wwwroot/imagesThumbnails"))
        {
            Directory.CreateDirectory("wwwroot/imagesThumbnails");
        }
        if (!Directory.Exists("wwwroot/models"))
        {
            Directory.CreateDirectory("wwwroot/models");
        }
        if (!Directory.Exists("wwwroot/projetDocuments"))
        {
            Directory.CreateDirectory("wwwroot/projetDocuments");
        }
        if (!Directory.Exists("wwwroot/itemDocuments"))
        {
            Directory.CreateDirectory("wwwroot/itemDocuments");
        }
        if (!Directory.Exists("wwwroot/commandDocuments"))
        {
            Directory.CreateDirectory("wwwroot/commandDocuments");
        }
    }

    private static void InitializeDatabase(WebApplication app)
    {
        using (var serviceScope = app.Services.CreateScope())
        {
            var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            // check if the database is up to date with the migrations
            var pendingMigrations = context.Database.GetPendingMigrations();
            if (pendingMigrations.Any())
            {
                // Appliquer les migrations si nécessaire
                context.Database.Migrate();
            }
            // check if the database is empty
            if (!context.Users.Any())
            {
                var userService = serviceScope.ServiceProvider.GetRequiredService<IUserService>();
                userService.CreateFirstAdminUser(new CreateUserDto
                {
                    nom_user = "Admin",
                    prenom_user = "Admin",
                    email_user = "admin@localhost.local",
                    mdp_user = "Admin@1234",
                    role_user = UserRole.Admin
                }).Wait();
            }
        }
    }
}
