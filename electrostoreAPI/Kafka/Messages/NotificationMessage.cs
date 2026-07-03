namespace ElectrostoreAPI.Kafka.Messages;

public class NotificationMessage
{
    public List<string> Types { get; set; } = ["email"];
    public string? RecipientEmail { get; set; }
    public int? RecipientUserId { get; set; }
    public string? TemplateId { get; set; }
    public string? Language { get; set; }
    public Dictionary<string, string>? TemplateValues { get; set; }
    public string? Subject { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public Dictionary<string, string>? PushData { get; set; }
}