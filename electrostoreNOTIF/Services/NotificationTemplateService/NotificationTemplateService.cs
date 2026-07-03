using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace ElectrostoreNOTIF.Services.NotificationTemplateService;

public class NotificationTemplateService : INotificationTemplateService
{
    // --- Regex compilées une seule fois ---

    /// <summary>Blocs {{#each key}}…{{/each}}, y compris sur plusieurs lignes.</summary>
    private static readonly Regex EachBlockRegex = new(
        @"\{\{#each\s+(\w+)\}\}(.*?)\{\{/each\}\}",
        RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>Placeholder scalaire {{key}} ou {{ key }}.</summary>
    private static readonly Regex PlaceholderRegex = new(
        @"\{\{\s*(\w+)\s*\}\}",
        RegexOptions.Compiled);

    /// <summary>Placeholder courant {{.}} dans un bloc #each.</summary>
    private static readonly Regex DotPlaceholderRegex = new(
        @"\{\{\s*\.\s*\}\}",
        RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // --- État interne ---

    /// <summary>Index construit au démarrage : "templateId:langue" → chemin de fichier.</summary>
    private readonly IReadOnlyDictionary<string, string> _index;

    /// <summary>Cache des templates déjà lus : chargement à la demande.</summary>
    private readonly ConcurrentDictionary<string, NotificationTemplate> _cache = new(StringComparer.OrdinalIgnoreCase);

    private readonly string _defaultLanguage;
    private readonly ILogger<NotificationTemplateService> _logger;

    public NotificationTemplateService(IConfiguration configuration, ILogger<NotificationTemplateService> logger)
    {
        _logger = logger;
        _defaultLanguage = configuration["NotificationTemplates:DefaultLanguage"] ?? "fr";
        _index = BuildIndex();
        _logger.LogInformation("NotificationTemplateService: {Count} template(s) indexé(s).", _index.Count);
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    public NotificationTemplateRender? RenderTemplate(
        string templateId,
        IDictionary<string, JsonElement>? values,
        string? language)
    {
        if (string.IsNullOrWhiteSpace(templateId))
            return null;

        var lang = NormalizeLanguage(language) ?? _defaultLanguage;
        var template = ResolveTemplate(templateId, lang);
        if (template is null)
            return null;

        return new NotificationTemplateRender
        {
            Subject  = RenderContent(template.Subject, values),
            Body     = RenderContent(template.Body,    values),
            Title    = RenderContent(template.Title,   values),
            Data     = template.Data?.ToDictionary(
                           kvp => kvp.Key,
                           kvp => RenderContent(kvp.Value, values) ?? string.Empty),
            Language = template.Language
        };
    }

    // -----------------------------------------------------------------------
    // Résolution du template (cache → index → langue par défaut)
    // -----------------------------------------------------------------------

    private NotificationTemplate? ResolveTemplate(string templateId, string language)
    {
        var key = IndexKey(templateId, language);

        // 1. Cache pour la langue demandée
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        // 2. Index pour la langue demandée
        if (_index.TryGetValue(key, out var filePath))
            return LoadAndCache(templateId, language, filePath);

        // 3. Repli sur la langue par défaut
        if (!string.Equals(language, _defaultLanguage, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug(
                "Template '{Id}' introuvable pour '{Lang}', repli sur '{Default}'.",
                templateId, language, _defaultLanguage);

            var defaultKey = IndexKey(templateId, _defaultLanguage);

            if (_cache.TryGetValue(defaultKey, out cached))
                return cached;

            if (_index.TryGetValue(defaultKey, out filePath))
                return LoadAndCache(templateId, _defaultLanguage, filePath);
        }

        _logger.LogWarning(
            "Template '{Id}' introuvable (langue='{Lang}', défaut='{Default}').",
            templateId, language, _defaultLanguage);
        return null;
    }

    private NotificationTemplate? LoadAndCache(string templateId, string language, string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var template = JsonSerializer.Deserialize<NotificationTemplate>(json, JsonOptions);
            if (template is null)
            {
                _logger.LogWarning("Template '{Id}' désérialisé en null depuis '{Path}'.", templateId, filePath);
                return null;
            }

            template.TemplateId = templateId;
            template.Language   = language;

            // TryAdd est thread-safe ; si une autre thread a déjà mis en cache, on ignore.
            _cache.TryAdd(IndexKey(templateId, language), template);
            _logger.LogDebug("Template '{Id}' ({Lang}) chargé depuis '{Path}'.", templateId, language, filePath);
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Impossible de charger le template '{Id}' depuis '{Path}'.", templateId, filePath);
            return null;
        }
    }

    private static string IndexKey(string templateId, string language)
        => $"{templateId}:{language}";

    // -----------------------------------------------------------------------
    // Construction de l'index au démarrage (lecture des noms de fichiers uniquement)
    // -----------------------------------------------------------------------

    private IReadOnlyDictionary<string, string> BuildIndex()
    {
        var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var templateRoot = Path.Combine(AppContext.BaseDirectory, "Templates");

        if (!Directory.Exists(templateRoot))
        {
            _logger.LogWarning("Dossier de templates introuvable : '{Path}'.", templateRoot);
            return index;
        }

        foreach (var langDir in Directory.EnumerateDirectories(templateRoot))
        {
            var lang = Path.GetFileName(langDir).ToLowerInvariant();

            foreach (var file in Directory.EnumerateFiles(langDir, "*.json", SearchOption.TopDirectoryOnly))
            {
                var id  = Path.GetFileNameWithoutExtension(file);
                var key = IndexKey(id, lang);

                if (!index.ContainsKey(key))
                {
                    index[key] = file;
                }
                else
                {
                    _logger.LogWarning("Template dupliqué ignoré : '{File}' (clé '{Key}' déjà présente).", file, key);
                }
            }
        }
        return index;
    }

    private static string? NormalizeLanguage(string? language)
        => string.IsNullOrWhiteSpace(language) ? null : language.Trim().ToLowerInvariant();

    // -----------------------------------------------------------------------
    // Moteur de rendu
    // -----------------------------------------------------------------------

    /// <summary>
    /// Traite un texte de template :
    ///   1. Remplace les blocs {{#each key}}…{{/each}} par l'itération du tableau.
    ///   2. Remplace les scalaires {{key}}.
    /// </summary>
    private static string? RenderContent(string? text, IDictionary<string, JsonElement>? values)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        if (values is null || values.Count == 0)
            return text;

        // Étape 1 : blocs #each
        text = EachBlockRegex.Replace(text, match =>
        {
            var key          = match.Groups[1].Value;
            var innerTemplate = match.Groups[2].Value;

            if (!values.TryGetValue(key, out var element) || element.ValueKind != JsonValueKind.Array)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var item in element.EnumerateArray())
                sb.Append(RenderItem(innerTemplate, item, values));

            return sb.ToString();
        });

        // Étape 2 : scalaires
        text = PlaceholderRegex.Replace(text, match =>
        {
            var key = match.Groups[1].Value;
            return values.TryGetValue(key, out var element)
                ? ElementToString(element) ?? match.Value
                : match.Value;
        });

        return text;
    }

    /// <summary>
    /// Remplace les placeholders dans le corps d'un bloc #each pour un élément donné.
    /// - Élément scalaire : {{.}} → valeur
    /// - Élément objet    : {{prop}} → valeur de la propriété
    /// Les placeholders non résolus dans l'item sont cherchés dans les valeurs globales.
    /// </summary>
    private static string RenderItem(
        string innerTemplate,
        JsonElement item,
        IDictionary<string, JsonElement>? globalValues)
    {
        // Remplacement de {{.}} pour les scalaires
        var result = DotPlaceholderRegex.Replace(innerTemplate, _ => ElementToString(item) ?? string.Empty);

        // Remplacement de {{prop}} : propriété de l'objet courant, puis valeur globale en repli
        result = PlaceholderRegex.Replace(result, match =>
        {
            var key = match.Groups[1].Value;

            if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty(key, out var prop))
                return ElementToString(prop) ?? match.Value;

            if (globalValues is not null && globalValues.TryGetValue(key, out var global))
                return ElementToString(global) ?? match.Value;

            return match.Value;
        });

        return result;
    }

    /// <summary>Convertit un JsonElement scalaire en string ; retourne null pour les types non supportés.</summary>
    private static string? ElementToString(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String  => element.GetString(),
        JsonValueKind.Number  => element.GetRawText(),
        JsonValueKind.True    => "true",
        JsonValueKind.False   => "false",
        JsonValueKind.Null    => string.Empty,
        _                     => null
    };
}

// -----------------------------------------------------------------------
// Modèles
// -----------------------------------------------------------------------

public sealed class NotificationTemplate
{
    public string? TemplateId { get; set; }
    public string? Language   { get; set; }
    public string? Subject    { get; set; }
    public string? Body       { get; set; }
    public string? Title      { get; set; }
    public Dictionary<string, string>? Data { get; set; }
}

public sealed class NotificationTemplateRender
{
    public string? Subject  { get; set; }
    public string? Body     { get; set; }
    public string? Title    { get; set; }
    public string? Language { get; set; }
    public Dictionary<string, string>? Data { get; set; }
}

