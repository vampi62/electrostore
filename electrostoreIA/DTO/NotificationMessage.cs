namespace ElectrostoreIA.Dto;

public class NotificationMessage
{
    public List<string> Types { get; set; } = ["email"];
    public string? RecipientEmail { get; set; }
    public int? RecipientUserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string>? PushData { get; set; }
}