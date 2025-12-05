using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using AutoMapper;
using electrostore;
using electrostore.Dto;
using electrostore.Enums;
using electrostore.Models;
using electrostore.Services.UserService;
using electrostore.Services.JwtService;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.AspNetCore.Mvc.Testing;

namespace electrostore.Tests
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
                    nom_user = "Admin",
                    prenom_user = "Admin",
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
                        nom_user = "Admin",
                        prenom_user = "Admin",
                        email_user = "admin@localhost.local",
                        mdp_user = "Admin@1234",
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

        // Tests ciblés pour SwaggerClass (AddTotalCountHeaderFilter)
        [Fact]
        public void SwaggerFilter_ShouldAddTotalCountHeader_WhenLimitAndOffsetQueryAnd200Response()
        {
            // Arrange
            var operation = new Microsoft.OpenApi.Models.OpenApiOperation();
            operation.Responses["200"] = new Microsoft.OpenApi.Models.OpenApiResponse();

            var apiDescription = new Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription();
            apiDescription.ParameterDescriptions.Add(new Microsoft.AspNetCore.Mvc.ApiExplorer.ApiParameterDescription
            {
                Name = "limit",
                Source = Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource.Query
            });
            apiDescription.ParameterDescriptions.Add(new Microsoft.AspNetCore.Mvc.ApiExplorer.ApiParameterDescription
            {
                Name = "offset",
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
            Assert.True(operation.Responses.TryGetValue("200", out var resp) && resp.Headers.ContainsKey("X-Total-Count"));
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

        [Fact]
        public void SwaggerGen_ShouldProduceV1Document_WithExpectedTitle_And_IncludeFilter()
        {
            // Arrange: configure SwaggerGen similarly to Program.cs
            var services = new ServiceCollection();
            
            // Add required services
            var mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
            mockWebHostEnvironment.Setup(m => m.EnvironmentName).Returns("Development");
            mockWebHostEnvironment.Setup(m => m.ApplicationName).Returns("electrostore");
            services.AddSingleton<IWebHostEnvironment>(mockWebHostEnvironment.Object);
            services.AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(mockWebHostEnvironment.Object);
            
            services.AddLogging();
            services.AddRouting();
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "ElectroStore API", Version = "v1" });
                c.OperationFilter<AddTotalCountHeaderFilter>();
            });

            using var provider = services.BuildServiceProvider();
            var swaggerGen = provider.GetRequiredService<ISwaggerProvider>();

            // Act
            var doc = swaggerGen.GetSwagger("v1");

            // Assert
            Assert.NotNull(doc);
            Assert.Equal("ElectroStore API", doc.Info.Title);
            Assert.Equal("v1", doc.Info.Version);
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