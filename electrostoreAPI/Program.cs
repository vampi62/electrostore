using Microsoft.EntityFrameworkCore;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Minio;

using MQTTnet;
using MQTTnet.Client;

using electrostore.Dto;
using electrostore.Enums;

using electrostore.Services.BoxService;
using electrostore.Services.BoxTagService;
using electrostore.Services.CameraService;
using electrostore.Services.CommandCommentaireService;
using electrostore.Services.CommandDocumentService;
using electrostore.Services.CommandItemService;
using electrostore.Services.CommandService;
using electrostore.Services.ConfigService;
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
using electrostore.Services.ProjetService;
using electrostore.Services.SessionService;
using electrostore.Services.SmtpService;
using electrostore.Services.StoreService;
using electrostore.Services.StoreTagService;
using electrostore.Services.TagService;
using electrostore.Services.UserService;
using electrostore.Services.JwtService;
using electrostore.Services.FileService;
using electrostore.Services.AuthService;
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
                    var modelStateErrors = context.ModelState
                        .Where(ms => ms.Value != null && ms.Value.Errors.Count > 0)
                        .SelectMany(ms => ms.Value.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();


                    if (modelStateErrors.Any())
                    {
                        var errorResponse = new
                        {
                            Error = "Incorrect call",
                        };

                        return new BadRequestObjectResult(errorResponse);
                    }

                    return new BadRequestResult();
                };
            });

        builder.Services.AddAutoMapper(typeof(MappingProfile));
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

        app.MapControllers();

        app.UseMiddleware<ExceptionsHandler>();

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
                        if (context.SecurityToken is not JwtSecurityToken token)
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
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("RefreshToken", policy =>
                policy.RequireRole("refresh"));
            options.AddPolicy("AccessToken", policy =>
                policy.RequireRole("access"));
        });

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
            var factory = new MqttFactory();
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
                .Build();
            return minioClient;
        });

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
        builder.Services.AddScoped<IProjetService, ProjetService>();
        builder.Services.AddScoped<ISessionService, SessionService>();
        builder.Services.AddScoped<ISmtpService, SmtpService>();
        builder.Services.AddScoped<IStoreService, StoreService>();
        builder.Services.AddScoped<IStoreTagService, StoreTagService>();
        builder.Services.AddScoped<ITagService, TagService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IJwiService, JwiService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
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
