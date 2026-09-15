using System.Collections.Immutable;

namespace ElectrostoreAPI.Constants;

public static class FieldLengths
{
    public const int MaxUrlLength = 2048;
    public const int MaxUrlFileLength = 300;
    public const int MaxCommentLength = 455;
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
    public const int MaxChatMessageLength = 4000;
    public static int MaxDocumentSizeMB { get; private set; } = 5; // in MB
    public static int MaxImageSizeMB { get; private set; } = 5; // in MB
    public static int MaxAudioSizeMB { get; private set; } = 25; // in MB
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
    public static ImmutableDictionary<string, string> AllowedAudioMimeTypes { get; private set; } = ImmutableDictionary.CreateRange<string, string>(new[]
    {
        KeyValuePair.Create("audio/mpeg", ".mp3"),
        KeyValuePair.Create("audio/mp3", ".mp3"),
        KeyValuePair.Create("audio/wav", ".wav"),
        KeyValuePair.Create("audio/x-wav", ".wav"),
        KeyValuePair.Create("audio/webm", ".webm"),
        KeyValuePair.Create("audio/ogg", ".ogg"),
        KeyValuePair.Create("audio/mp4", ".m4a"),
        KeyValuePair.Create("audio/m4a", ".m4a")
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
        MaxAudioSizeMB = configuration.GetValue<int>("FileValidation:MaxAudioSizeMB", MaxAudioSizeMB);
        var audioMimeTypes = configuration.GetSection("FileValidation:AllowedAudioMimeTypes").Get<Dictionary<string, string>>();
        if (audioMimeTypes != null && audioMimeTypes.Count > 0)
            AllowedAudioMimeTypes = ImmutableDictionary.CreateRange(audioMimeTypes);
    }
}
