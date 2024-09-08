using Microsoft.EntityFrameworkCore;
using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;

using MQTTnet;
using MQTTnet.Client;

using electrostore;

using electrostore.Models;

using electrostore.Services.BoxService;
using electrostore.Services.BoxTagService;
using electrostore.Services.CameraService;
using electrostore.Services.CommandCommentaireService;
using electrostore.Services.CommandItemService;
using electrostore.Services.CommandService;
using electrostore.Services.IAImgService;
using electrostore.Services.IAService;
using electrostore.Services.ImgService;
using electrostore.Services.ItemBoxService;
using electrostore.Services.ItemService;
using electrostore.Services.ItemTagService;
using electrostore.Services.LedService;
using electrostore.Services.ProjetCommentaireService;
using electrostore.Services.ProjetItemService;
using electrostore.Services.ProjetService;
using electrostore.Services.StoreService;
using electrostore.Services.StoreTagService;
using electrostore.Services.TagService;
using electrostore.Services.UserService;
using electrostore.Services.JwtService;

using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
var key = Encoding.ASCII.GetBytes(jwtSettings.Key);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), new MySqlServerVersion(new Version(8,0,19))));

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

addScopes(builder);

builder.Services.AddControllers(options => { options.Filters.Add(new AuthorizeFilter()); })
    // Invalid model state response factory
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var modelStateErrors = context.ModelState
                .Where(ms => ms.Value.Errors.Count > 0)
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

builder.Services.AddControllers();
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
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
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
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("admin", policy =>
        policy.RequireRole("admin"));
    options.AddPolicy("user", policy =>
        policy.RequireRole("user"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()
            //.WithOrigins("https://store.raspberrycloudav.fr")
            //.AllowCredentials()
            );
});

var app = builder.Build();
//if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
//}
app.UseStaticFiles();

if (!Directory.Exists("wwwroot/images"))
{
    Directory.CreateDirectory("wwwroot/images");
}
if (!Directory.Exists("wwwroot/models"))
{
    Directory.CreateDirectory("wwwroot/models");
}

app.UseCors("CorsPolicy");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

void addScopes(WebApplicationBuilder builder)
{
    builder.Services.AddScoped<IBoxService, BoxService>();
    builder.Services.AddScoped<IBoxTagService, BoxTagService>();
    builder.Services.AddScoped<ICameraService, CameraService>();
    builder.Services.AddScoped<ICommandCommentaireService, CommandCommentaireService>();
    builder.Services.AddScoped<ICommandItemService, CommandItemService>();
    builder.Services.AddScoped<ICommandService, CommandService>();
    builder.Services.AddScoped<IIAImgService, IAImgService>();
    builder.Services.AddScoped<IIAService, IAService>();
    builder.Services.AddScoped<IImgService, ImgService>();
    builder.Services.AddScoped<IItemBoxService, ItemBoxService>();
    builder.Services.AddScoped<IItemService, ItemService>();
    builder.Services.AddScoped<IItemTagService, ItemTagService>();
    builder.Services.AddScoped<ILedService, LedService>();
    builder.Services.AddScoped<IProjetCommentaireService, ProjetCommentaireService>();
    builder.Services.AddScoped<IProjetItemService, ProjetItemService>();
    builder.Services.AddScoped<IProjetService, ProjetService>();
    builder.Services.AddScoped<IStoreService, StoreService>();
    builder.Services.AddScoped<IStoreTagService, StoreTagService>();
    builder.Services.AddScoped<ITagService, TagService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddSingleton<JwtService>();
}