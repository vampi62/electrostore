using System.ComponentModel.DataAnnotations;

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
}