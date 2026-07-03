using System.Collections.Immutable;

namespace ElectrostoreAPI.Dto;

public record ReadConfig
{
    public bool demo_mode { get; init; }
    public string app_language { get; init; } = "en";
    public int max_length_url { get; init; }
    public int max_length_commentaire { get; init; }
    public int max_length_description { get; init; }
    public int max_length_name { get; init; }
    public int max_length_type { get; init; }
    public int max_length_email { get; init; }
    public int max_length_location { get; init; }
    public int max_length_cron_expression { get; init; }
    public int max_length_ip { get; init; }
    public int max_length_reason { get; init; }
    public int max_length_status { get; init; }
    public int max_length_device_name { get; init; }
    public int max_length_push_key { get; init; }
    public int max_length_push_auth { get; init; }
    public int max_length_tracking_number { get; init; }
    public int max_length_carrier_name { get; init; }
    public int max_length_timezone { get; init; }
    public int max_length_coordinate { get; init; }
    public int max_length_postal_code { get; init; }
    public int max_size_document_in_mb { get; init; }
    public int max_size_image_in_mb { get; init; }
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
    public const int MaxUrlLength = 2048;
    public const int MaxUrlFileLength = 300;
    public const int MaxCommentaireLength = 455;
    public const int MaxDescriptionLength = 500;
    public const int MaxNameLength = 50;
    public const int MaxTypeLength = 150;
    public const int MaxEmailLength = 100;
    public const int MaxLocationLength = 100;
    public const int MaxCronExpressionLength = 100;
    public const int MaxIpLength = 50;
    public const int MaxReasonLength = 50;
    public const int MaxStatusLength = 50;
    public const int MaxDeviceNameLength = 255;
    public const int MaxPushKeyLength = 512;
    public const int MaxPushAuthLength = 256;
    public const int MaxTrackingNumberLength = 100;
    public const int MaxCarrierNameLength = 100;
    public const int MaxTimezoneLength = 50;
    public const int MaxCoordinateLength = 50;
    public const int MaxPostalCodeLength = 20;
    public static int MaxDocumentSizeMB { get; private set; } = 5; // in MB
    public static int MaxImageSizeMB { get; private set; } = 5; // in MB
    public static ImmutableDictionary<string, string> AllowedImageMimeTypes { get; private set; } = ImmutableDictionary.CreateRange<string, string>(new[]
    {
        KeyValuePair.Create("image/png", ".png"),
        KeyValuePair.Create("image/webp", ".webp"),
        KeyValuePair.Create("image/jpg", ".jpg"),
        KeyValuePair.Create("image/jpeg", ".jpeg"),
        KeyValuePair.Create("image/gif", ".gif"),
        KeyValuePair.Create("image/bmp", ".bmp")
    });
    public static ImmutableDictionary<string, string> AllowedDocumentMimeTypes { get; private set; } = ImmutableDictionary.CreateRange<string, string>(new[]
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

    public static void Initialize(IConfiguration configuration)
    {
        MaxDocumentSizeMB = configuration.GetValue<int>("FileValidation:MaxDocumentSizeMB", MaxDocumentSizeMB);
        var imageMimeTypes = configuration.GetSection("FileValidation:AllowedImageMimeTypes").Get<Dictionary<string, string>>();
        if (imageMimeTypes != null && imageMimeTypes.Count > 0)
            AllowedImageMimeTypes = ImmutableDictionary.CreateRange(imageMimeTypes);
        var documentMimeTypes = configuration.GetSection("FileValidation:AllowedDocumentMimeTypes").Get<Dictionary<string, string>>();
        if (documentMimeTypes != null && documentMimeTypes.Count > 0)
            AllowedDocumentMimeTypes = ImmutableDictionary.CreateRange(documentMimeTypes);
    }
}