using System.Text.Json;
using ElectrostoreNOTIF.Services.NotificationTemplateService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreNOTIF.Tests.Services;

public class NotificationTemplateServiceTests
{
    private static NotificationTemplateService CreateService(string defaultLanguage = "fr")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NotificationTemplates:DefaultLanguage"] = defaultLanguage
            })
            .Build();
        return new NotificationTemplateService(configuration, new Mock<ILogger<NotificationTemplateService>>().Object);
    }

    private static Dictionary<string, JsonElement> ParseValues(string json)
    {
        using var document = JsonDocument.Parse(json);
        var values = new Dictionary<string, JsonElement>();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            values[property.Name] = property.Value.Clone();
        }
        return values;
    }

    [Fact]
    public void RenderTemplate_ShouldReturnNull_WhenTemplateIdIsEmpty()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.RenderTemplate("", null, "en");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void RenderTemplate_ShouldReturnNull_WhenTemplateDoesNotExist()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.RenderTemplate("does-not-exist", null, "en");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void RenderTemplate_ShouldRenderScalarPlaceholders_FromRealTemplateFile()
    {
        // Arrange
        var service = CreateService();
        var values = ParseValues("""
        {
            "firstName": "Jean",
            "lastName": "Dupont",
            "role": "Admin"
        }
        """);

        // Act
        var result = service.RenderTemplate("account-created", values, "en");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Jean", result!.Body);
        Assert.Contains("Dupont", result.Body);
        Assert.Contains("Admin", result.Body);
        Assert.Equal("ElectroStore account created", result.Title);
        Assert.Equal("account_created", result.Data?["event"]);
    }

    [Fact]
    public void RenderTemplate_ShouldFallBackToDefaultLanguage_WhenRequestedLanguageIsUnavailable()
    {
        // Arrange
        var service = CreateService(defaultLanguage: "fr");
        var values = ParseValues("""
        {
            "firstName": "Jean",
            "lastName": "Dupont",
            "role": "Admin"
        }
        """);

        // Act
        var result = service.RenderTemplate("account-created", values, "es");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Bonjour", result!.Body);
    }

    [Fact]
    public void RenderTemplate_ShouldUseDefaultLanguage_WhenLanguageIsNull()
    {
        // Arrange
        var service = CreateService(defaultLanguage: "en");
        var values = ParseValues("""
        {
            "firstName": "Jean",
            "lastName": "Dupont",
            "role": "Admin"
        }
        """);

        // Act
        var result = service.RenderTemplate("account-created", values, null);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Hello", result!.Body);
    }

    [Fact]
    public void RenderTemplate_ShouldRenderEachBlock_WithObjectItemsAndGlobalFallback()
    {
        // Arrange
        var templateId = $"test-each-{Guid.NewGuid():N}";
        var templateDir = Path.Combine(AppContext.BaseDirectory, "Templates", "en");
        Directory.CreateDirectory(templateDir);
        var templatePath = Path.Combine(templateDir, $"{templateId}.json");
        File.WriteAllText(templatePath, """
        {
            "subject": "Order for {{name}}",
            "body": "<ul>{{#each items}}<li>{{itemName}} x{{qty}} ({{name}})</li>{{/each}}</ul>"
        }
        """);

        try
        {
            var service = CreateService(defaultLanguage: "en");
            var values = ParseValues("""
            {
                "name": "Alice",
                "items": [
                    {"itemName": "Widget", "qty": 2},
                    {"itemName": "Gadget", "qty": 5}
                ]
            }
            """);

            // Act
            var result = service.RenderTemplate(templateId, values, "en");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Order for Alice", result!.Subject);
            Assert.Contains("<li>Widget x2 (Alice)</li>", result.Body);
            Assert.Contains("<li>Gadget x5 (Alice)</li>", result.Body);
        }
        finally
        {
            File.Delete(templatePath);
        }
    }
}
