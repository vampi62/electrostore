using System.Collections.Immutable;

namespace electrostore.Dto;

public record ReadConfig
{
    public bool smtp_enabled { get; init; }
    public bool mqtt_connected { get; init; }
    public required string ia_service_status { get; init; }
    public bool demo_mode { get; init; }
    public int max_length_url { get; init; }
    public int max_length_commentaire { get; init; }
    public int max_length_description { get; init; }
    public int max_length_name { get; init; }
    public int max_length_type { get; init; }
    public int max_length_email { get; init; }
    public int max_length_ip { get; init; }
    public int max_length_reason { get; init; }
    public int max_length_status { get; init; }
    public int max_size_document_in_mb { get; init; }
    public List<SSOAvailableProvider>? sso_available_providers { get; init; }
    public string[]? allowed_image_mime_types { get; init; }
    public string[]? allowed_image_extensions { get; init; }
    public string[]? allowed_document_mime_types { get; init; }
    public string[]? allowed_document_extensions { get; init; }
}

public record SSOAvailableProvider
{
    public required string provider { get; init; }
    public required string display_name { get; init; }
    public required string icon_url { get; init; }
}

public static class Constants
{
    public const int MaxUrlLength = 150;
    public const int MaxCommentaireLength = 455;
    public const int MaxDescriptionLength = 500;
    public const int MaxNameLength = 50;
    public const int MaxTypeLength = 150;
    public const int MaxEmailLength = 100;
    public const int MaxIpLength = 50;
    public const int MaxReasonLength = 50;
    public const int MaxStatusLength = 50;
    public const int MaxDocumentSizeMB = 5; // in MB
    public static readonly ImmutableDictionary<string, string> AllowedImageMimeTypes = ImmutableDictionary.CreateRange<string, string>(new[]
    {
        KeyValuePair.Create("image/png", ".png"),
        KeyValuePair.Create("image/webp", ".webp"),
        KeyValuePair.Create("image/jpg", ".jpg"),
        KeyValuePair.Create("image/jpeg", ".jpeg"),
        KeyValuePair.Create("image/gif", ".gif"),
        KeyValuePair.Create("image/bmp", ".bmp")
    });
    public static readonly ImmutableDictionary<string, string> AllowedDocumentMimeTypes = ImmutableDictionary.CreateRange<string, string>(new[]
    {
        KeyValuePair.Create("application/pdf", ".pdf"),
        KeyValuePair.Create("application/msword", ".doc"),
        KeyValuePair.Create("application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx"),
        KeyValuePair.Create("application/vnd.ms-excel", ".xls"),
        KeyValuePair.Create("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx"),
        KeyValuePair.Create("application/vnd.ms-powerpoint", ".ppt"),
        KeyValuePair.Create("application/vnd.openxmlformats-officedocument.presentationml.presentation", ".pptx"),
        KeyValuePair.Create("text/plain", ".txt"),
        KeyValuePair.Create("application/zip", ".zip"),
        KeyValuePair.Create("application/x-rar-compressed", ".rar"),
        KeyValuePair.Create("image/png", ".png"),
        KeyValuePair.Create("image/webp", ".webp"),
        KeyValuePair.Create("image/jpg", ".jpg"),
        KeyValuePair.Create("image/jpeg", ".jpeg"),
        KeyValuePair.Create("image/gif", ".gif"),
        KeyValuePair.Create("image/bmp", ".bmp")
    });
}