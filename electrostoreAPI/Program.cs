using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTnet.Client;
using System.Net;
using System.Net.Mail;

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

using System.ComponentModel;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), new MySqlServerVersion(new Version(8,0,19))));

builder.Services.AddSingleton<IMqttClient>(sp =>
{
    var factory = new MqttFactory();
    var mqttClient = factory.CreateMqttClient();
    var options = new MqttClientOptionsBuilder()
        .WithClientId("ClientID")
        .WithTcpServer("mqtt.example.com", 1883)
        .WithCredentials("username", "password")
        .WithCleanSession()
        .Build();
    mqttClient.ConnectAsync(options);
    return mqttClient;
});

addScopes(builder);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins("https://store.raspberrycloudav.fr")
            .AllowCredentials());
});

var app = builder.Build();
//if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
//}
app.UseStaticFiles();

app.UseCors("CorsPolicy");

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
}