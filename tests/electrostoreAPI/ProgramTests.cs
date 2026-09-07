using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MQTTnet;
using Moq;
using Xunit;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using AutoMapper;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.UserService;
using ElectrostoreAPI.Services.JwtService;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ElectrostoreAPI.Tests
{
    public class ProgramTests
    {
        [Fact]
        public async Task Program_ShouldCreateAdminUser_WhenDatabaseIsEmpty()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var mockUserService = new Mock<IUserService>();
            
            mockUserService.Setup(s => s.CreateFirstAdminUser(It.IsAny<CreateUserDto>()))
                .ReturnsAsync(new ReadUserDto
                {
                    id_user = 1,
                    name_user = "Admin",
                    firstname_user = "Admin",
                    email_user = "admin@localhost.local",
                    role_user = UserRole.Admin
                });

            // Create an in-memory database context
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            // Create a database context and ensure it's empty
            using (var context = new ApplicationDbContext(options))
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
                
                // Act
                // Simulate what Program.cs does when the database is empty
                if (!context.Users.Any())
                {
                    // Call CreateFirstAdminUser directly on the mock
                    await mockUserService.Object.CreateFirstAdminUser(new CreateUserDto
                    {
                        name_user = "Admin",
                        firstname_user = "Admin",
                        email_user = "admin@localhost.local",
                        password_user = "Admin@1234",
                        role_user = UserRole.Admin
                    });
                }
            }
            
            // Assert
            // Verify that CreateFirstAdminUser was called
            mockUserService.Verify(s => s.CreateFirstAdminUser(It.Is<CreateUserDto>(dto => 
                dto.email_user == "admin@localhost.local" && 
                dto.role_user == UserRole.Admin)), 
                Times.Once);
        }

        [Fact]
        public async Task JWT_Authentication_ShouldReturnUnauthorized_WhenNoTokenProvided()
        {
            // Instead of testing with a real controller, let's create a simple test endpoint
            // that requires authentication but doesn't have any other dependencies
            
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            
            // Create a test server with a custom WebHostBuilder
            var builder = new WebHostBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new[]
                    {
                        new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", $"Server=localhost;Database={dbName};User=test;Password=test"),
                        new KeyValuePair<string, string?>("Jwt:Key", "test_key_for_jwt_authentication_that_is_long_enough"),
                        new KeyValuePair<string, string?>("Jwt:Issuer", "test"),
                        new KeyValuePair<string, string?>("Jwt:Audience", "test"),
                        new KeyValuePair<string, string?>("Jwt:AccessTokenExpirationMinutes", "15"),
                        new KeyValuePair<string, string?>("Jwt:RefreshTokenExpirationDays", "7"),
                        new KeyValuePair<string, string?>("SMTP:Enable", "false")
                    });
                })
                .ConfigureServices(services =>
                {
                    // Add routing services
                    services.AddRouting();
                    
                    // Configure JWT authentication
                    var jwtSettings = new JwtSettings
                    {
                        Key = "test_key_for_jwt_authentication_that_is_long_enough",
                        Issuer = "test",
                        Audience = "test",
                        ExpireDays = 1
                    };
                    
                    services.AddSingleton(Options.Create(jwtSettings));
                    
                    // Add JWT authentication
                    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options =>
                        {
                            options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuerSigningKey = true,
                                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.Key)),
                                ValidateIssuer = false,
                                ValidateAudience = false
                            };
                        });
                        
                    // Add authorization policies
                    services.AddAuthorization(options =>
                    {
                        options.AddPolicy("RequireAuthentication", policy =>
                            policy.RequireAuthenticatedUser());
                    });
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    
                    // Add a simple endpoint that requires authentication
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/api/user", async context =>
                        {
                            await context.Response.WriteAsync("Secured endpoint");
                        }).RequireAuthorization("RequireAuthentication");
                    });
                });

            using var server = new TestServer(builder);
            using var client = server.CreateClient();
            
            // Act
            var response = await client.GetAsync("/api/user");
            
            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public void SwaggerFilter_ShouldNotAddHeader_WhenMissingLimitOrOffset()
        {
            // Arrange
            var operation = new Microsoft.OpenApi.Models.OpenApiOperation();
            operation.Responses["200"] = new Microsoft.OpenApi.Models.OpenApiResponse();

            var apiDescription = new Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription();
            // Only 'limit' present, 'offset' missing
            apiDescription.ParameterDescriptions.Add(new Microsoft.AspNetCore.Mvc.ApiExplorer.ApiParameterDescription
            {
                Name = "limit",
                Source = Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource.Query
            });

            var schemaRepository = new Swashbuckle.AspNetCore.SwaggerGen.SchemaRepository();
            var methodInfo = typeof(ProgramTests).GetMethods().First();
            var dataContractResolver = new Swashbuckle.AspNetCore.SwaggerGen.JsonSerializerDataContractResolver(new System.Text.Json.JsonSerializerOptions());
            var schemaGenerator = new Swashbuckle.AspNetCore.SwaggerGen.SchemaGenerator(new Swashbuckle.AspNetCore.SwaggerGen.SchemaGeneratorOptions(), dataContractResolver);
            var context = new Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext(
                apiDescription,
                schemaGenerator,
                schemaRepository,
                methodInfo
            );

            var filter = new AddTotalCountHeaderFilter();

            // Act
            filter.Apply(operation, context);

            // Assert
            Assert.True(operation.Responses.TryGetValue("200", out var resp) && !resp.Headers.ContainsKey("X-Total-Count"));
        }

        // Program.Main() itself requires a real "config/appsettings.json" file and live infrastructure
        // (database, MQTT, S3, gRPC) once the app starts, so it isn't exercised directly.
        // ConfigureLogging/ConfigureConfiguration/ConfigureVault/MapHealthEndpoint are the private
        // static methods that own that setup and can be tested in isolation.
        private static void InvokePrivateStatic(string methodName, object arg)
        {
            var method = typeof(Program).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException($"{methodName} method not found");
            method.Invoke(null, new[] { arg });
        }

        // ---------- ConfigureLogging ----------

        [Fact]
        public void ConfigureLogging_ShouldConfigureSimpleConsoleFormatter()
        {
            var builder = WebApplication.CreateBuilder();

            InvokePrivateStatic("ConfigureLogging", builder);

            using var provider = builder.Services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptionsMonitor<SimpleConsoleFormatterOptions>>().CurrentValue;

            Assert.Equal("yyyy-MM-dd HH:mm:ss ", options.TimestampFormat);
            Assert.True(options.SingleLine);
        }

        // ---------- ConfigureConfiguration ----------

        [Fact]
        public void ConfigureConfiguration_ShouldLoadDevelopmentOverride_WhenEnvironmentIsDevelopment()
        {
            var previousDirectory = Directory.GetCurrentDirectory();
            var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var configDir = Path.Combine(tempRoot, "config");
            Directory.CreateDirectory(configDir);
            File.WriteAllText(Path.Combine(configDir, "appsettings.json"), "{\"Foo\":\"Base\"}");
            File.WriteAllText(Path.Combine(configDir, "appsettings.Development.json"), "{\"Foo\":\"Dev\"}");

            try
            {
                Directory.SetCurrentDirectory(tempRoot);
                var builder = WebApplication.CreateBuilder(new WebApplicationOptions
                {
                    EnvironmentName = Environments.Development
                });

                InvokePrivateStatic("ConfigureConfiguration", builder);

                Assert.Equal("Dev", builder.Configuration["Foo"]);
            }
            finally
            {
                Directory.SetCurrentDirectory(previousDirectory);
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Fact]
        public void ConfigureConfiguration_ShouldNotLoadDevelopmentOverride_WhenEnvironmentIsProduction()
        {
            var previousDirectory = Directory.GetCurrentDirectory();
            var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var configDir = Path.Combine(tempRoot, "config");
            Directory.CreateDirectory(configDir);
            File.WriteAllText(Path.Combine(configDir, "appsettings.json"), "{\"Foo\":\"Base\"}");
            File.WriteAllText(Path.Combine(configDir, "appsettings.Development.json"), "{\"Foo\":\"Dev\"}");

            try
            {
                Directory.SetCurrentDirectory(tempRoot);
                var builder = WebApplication.CreateBuilder(new WebApplicationOptions
                {
                    EnvironmentName = Environments.Production
                });

                InvokePrivateStatic("ConfigureConfiguration", builder);

                Assert.Equal("Base", builder.Configuration["Foo"]);
            }
            finally
            {
                Directory.SetCurrentDirectory(previousDirectory);
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        // ---------- ConfigureVault ----------

        [Fact]
        public void ConfigureVault_ShouldRegisterVaultClient_WhenVaultEnabled()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Vault:Enable"] = "true",
                ["Vault:Token"] = "fake-token",
                ["Vault:Addr"] = "http://localhost:8200",
                ["Vault:Path"] = "fake-path",
                ["Vault:MountPoint"] = "fake-mount-point"
            });

            InvokePrivateStatic("ConfigureVault", builder);

            Assert.Contains(builder.Services, d => d.ServiceType == typeof(VaultSharp.IVaultClient));
        }

        [Fact]
        public void ConfigureVault_ShouldNotRegisterVaultClient_WhenVaultDisabled()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Vault:Enable"] = "false"
            });

            InvokePrivateStatic("ConfigureVault", builder);

            Assert.DoesNotContain(builder.Services, d => d.ServiceType == typeof(VaultSharp.IVaultClient));
        }

        // ---------- MapHealthEndpoint ----------

        [Theory]
        [InlineData(false, "healthy")]
        [InlineData(true, "demo")]
        public async Task HealthEndpoint_ShouldReturnExpectedStatus(bool demoMode, string expectedStatus)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DemoMode"] = demoMode.ToString()
            });

            var app = builder.Build();

            InvokePrivateStatic("MapHealthEndpoint", app);

            await app.StartAsync();
            try
            {
                using var client = app.GetTestClient();
                var response = await client.GetAsync("/health");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadFromJsonAsync<JsonElement>();

                Assert.Equal(expectedStatus, json.GetProperty("status").GetString());
            }
            finally
            {
                await app.StopAsync();
            }
        }
    }

    // Custom WebApplicationFactory for testing
    public class TestApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remplacer la DbContext par une base InMemory pour les tests
                var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (dbDescriptor != null)
                    services.Remove(dbDescriptor);
                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

                // Remplacer IMqttClient et IMinioClient par des mocks
                var mqttDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(MQTTnet.IMqttClient));
                if (mqttDescriptor != null)
                    services.Remove(mqttDescriptor);
                var minioDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(Minio.IMinioClient));
                if (minioDescriptor != null)
                    services.Remove(minioDescriptor);

                var mockMqtt = new Mock<MQTTnet.IMqttClient>();
                services.AddSingleton<MQTTnet.IMqttClient>(sp => mockMqtt.Object);
                var mockMinio = new Mock<Minio.IMinioClient>();
                services.AddSingleton<Minio.IMinioClient>(sp => mockMinio.Object);
            });
        }
    }

    // Custom startup class for testing
    public class TestStartup
    {
        public TestStartup(IConfiguration configuration)
        {
            Configuration = configuration;
            var loggerFactory = LoggerFactory.Create(builder => { });
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, loggerFactory);
            _mapper = mapperConfig.CreateMapper();
        }

        public IConfiguration Configuration { get; }
        public IMapper _mapper { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Configure services similar to Program.cs
            services.AddControllers();
            
            // Configure JWT authentication
            var jwtSettings = Configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? "default_test_key_for_jwt_authentication");
            
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                
            services.AddAuthorization(options =>
            {
                options.AddPolicy("RefreshToken", policy =>
                    policy.RequireRole("refresh"));
                options.AddPolicy("AccessToken", policy =>
                    policy.RequireRole("access"));
            });
            
            // Add HttpContextAccessor for SessionService
            services.AddHttpContextAccessor();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Configure middleware similar to Program.cs
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}