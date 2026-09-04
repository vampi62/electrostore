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

    [Theory]
    [InlineData("fr", "Rapport des mouvements de stock du 2026-08-28 au 2026-09-04", "Stock ajouté")]
    [InlineData("en", "Stock movement report from 2026-08-28 to 2026-09-04", "Stock added")]
    public void RenderTemplate_ShouldRenderTheMovementTable_FromRealTemplateFile(string language, string expectedSubject, string expectedType)
    {
        // Arrange
        var service = CreateService();
        var values = ParseValues($$"""
        {
            "firstName": "Jean",
            "lastName": "Dupont",
            "fromDate": "2026-08-28",
            "toDate": "2026-09-04",
            "movementCount": 2,
            "movements": [
                {
                    "date": "2026-09-01 08:30",
                    "item": "Resistor 10k",
                    "type": "{{expectedType}}",
                    "quantityChange": "+5",
                    "oldQuantity": 10,
                    "newQuantity": 15,
                    "user": "John Doe",
                    "notes": "restock"
                },
                {
                    "date": "2026-09-02 09:00",
                    "item": "Capacitor 100nF",
                    "type": "{{expectedType}}",
                    "quantityChange": "-3",
                    "oldQuantity": 20,
                    "newQuantity": 17,
                    "user": "-",
                    "notes": ""
                }
            ]
        }
        """);

        // Act
        var result = service.RenderTemplate("weekly-item-movement-report", values, language);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedSubject, result!.Subject);
        Assert.Contains("Jean", result.Body);
        Assert.Contains("<td>Resistor 10k</td>", result.Body);
        Assert.Contains("<td>Capacitor 100nF</td>", result.Body);
        Assert.Contains($"<td>{expectedType}</td>", result.Body);
        Assert.Contains(">+5</td>", result.Body);
        Assert.Contains(">17</td>", result.Body);
        Assert.DoesNotContain("{{", result.Body);
        Assert.Equal("weekly_item_movement_report", result.Data?["event"]);
        Assert.Equal("2", result.Data?["movementCount"]);
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

    private static string CreateTempTemplate(string language, string jsonContent, out string templateId)
    {
        templateId = $"test-{Guid.NewGuid():N}";
        var templateDir = Path.Combine(AppContext.BaseDirectory, "Templates", language);
        Directory.CreateDirectory(templateDir);
        var templatePath = Path.Combine(templateDir, $"{templateId}.json");
        File.WriteAllText(templatePath, jsonContent);
        return templatePath;
    }

    [Fact]
    public void RenderTemplate_ShouldReturnNull_WhenTemplateFileContainsMalformedJson()
    {
        // Arrange
        var templatePath = CreateTempTemplate("en", "{not-json", out var templateId);
        try
        {
            var service = CreateService(defaultLanguage: "en");

            // Act
            var result = service.RenderTemplate(templateId, null, "en");

            // Assert
            Assert.Null(result);
        }
        finally
        {
            File.Delete(templatePath);
        }
    }

    [Fact]
    public void RenderTemplate_ShouldReturnNull_WhenTemplateFileDeserializesToNull()
    {
        // Arrange
        var templatePath = CreateTempTemplate("en", "null", out var templateId);
        try
        {
            var service = CreateService(defaultLanguage: "en");

            // Act
            var result = service.RenderTemplate(templateId, null, "en");

            // Assert
            Assert.Null(result);
        }
        finally
        {
            File.Delete(templatePath);
        }
    }

    [Fact]
    public void RenderTemplate_ShouldReturnTextUnchanged_WhenValuesAreNull()
    {
        // Arrange
        var templatePath = CreateTempTemplate("en", """{"subject":"Hi {{name}}","body":"Body {{name}}"}""", out var templateId);
        try
        {
            var service = CreateService(defaultLanguage: "en");

            // Act
            var result = service.RenderTemplate(templateId, null, "en");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Hi {{name}}", result!.Subject);
            Assert.Equal("Body {{name}}", result.Body);
        }
        finally
        {
            File.Delete(templatePath);
        }
    }

    [Fact]
    public void RenderTemplate_ShouldLeavePlaceholderUnresolved_WhenKeyIsMissingFromValues()
    {
        // Arrange
        var templatePath = CreateTempTemplate("en", """{"subject":"Hi {{name}}, {{unknown}}"}""", out var templateId);
        try
        {
            var service = CreateService(defaultLanguage: "en");
            var values = ParseValues("""{"name":"Alice"}""");

            // Act
            var result = service.RenderTemplate(templateId, values, "en");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Hi Alice, {{unknown}}", result!.Subject);
        }
        finally
        {
            File.Delete(templatePath);
        }
    }

    [Fact]
    public void RenderTemplate_ShouldRenderEmptyString_WhenEachKeyIsMissingOrNotAnArray()
    {
        // Arrange
        var templatePath = CreateTempTemplate("en", """{"body":"[missing:{{#each missing}}{{.}}{{/each}}][notArray:{{#each name}}{{.}}{{/each}}]"}""", out var templateId);
        try
        {
            var service = CreateService(defaultLanguage: "en");
            var values = ParseValues("""{"name":"not-an-array"}""");

            // Act
            var result = service.RenderTemplate(templateId, values, "en");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("[missing:][notArray:]", result!.Body);
        }
        finally
        {
            File.Delete(templatePath);
        }
    }

    [Fact]
    public void RenderTemplate_ShouldRenderScalarAndObjectItems_AndFallBackToGlobalValues()
    {
        // Covers: scalar {{.}}, object {{.}} (renders empty), object property found,
        // object property missing falling back to a global value, and a placeholder that
        // resolves nowhere (kept literal). Also exercises number/bool/null scalar rendering.
        // Arrange
        var templatePath = CreateTempTemplate("en", """
        {
            "subject": "Count:{{count}} Active:{{active}} Nothing:{{nothing}}",
            "body": "<ul>{{#each items}}<li>{{.}}|{{itemName}}|{{sharedNote}}|{{missingProp}}</li>{{/each}}</ul>"
        }
        """, out var templateId);
        try
        {
            var service = CreateService(defaultLanguage: "en");
            var values = ParseValues("""
            {
                "count": 5,
                "active": true,
                "nothing": null,
                "sharedNote": "global-note",
                "items": ["scalarItem", {"itemName": "ObjItem"}]
            }
            """);

            // Act
            var result = service.RenderTemplate(templateId, values, "en");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Count:5 Active:true Nothing:", result!.Subject);
            Assert.Contains("<li>scalarItem|{{itemName}}|global-note|{{missingProp}}</li>", result.Body);
            Assert.Contains("<li>|ObjItem|global-note|{{missingProp}}</li>", result.Body);
        }
        finally
        {
            File.Delete(templatePath);
        }
    }

    [Fact]
    public void RenderTemplate_ShouldReturnSameResult_OnSecondCall_ServedFromCache()
    {
        // Arrange - the second RenderTemplate call for the same id/language is served from the
        // in-memory cache instead of re-reading/parsing the file.
        var templatePath = CreateTempTemplate("en", """{"subject":"Hi {{name}}"}""", out var templateId);
        try
        {
            var service = CreateService(defaultLanguage: "en");
            var values = ParseValues("""{"name":"Alice"}""");

            // Act
            var first = service.RenderTemplate(templateId, values, "en");
            var second = service.RenderTemplate(templateId, values, "en");

            // Assert
            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Equal(first!.Subject, second!.Subject);
        }
        finally
        {
            File.Delete(templatePath);
        }
    }
}
