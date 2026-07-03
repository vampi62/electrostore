using System.Text.Json;

namespace ElectrostoreNOTIF.Kafka.Messages;

public class NotificationMessage
{
    public List<string> Types { get; set; } = ["email"];
    public string? RecipientEmail { get; set; }
    public int? RecipientUserId { get; set; }
    public string? TemplateId { get; set; }
    public string? Language { get; set; }
    /// <summary>
    /// Valeurs des placeholders du template.
    /// Supporte les scalaires (string, number, bool) et les tableaux JSON
    /// pour les blocs {{#each key}}…{{/each}}.
    /// </summary>
    public Dictionary<string, JsonElement>? TemplateValues { get; set; }
    public string? Subject { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public Dictionary<string, string>? PushData { get; set; }
}
