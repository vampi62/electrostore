using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadConfig
{
    public bool smtp_enabled { get; init; }
    public bool mqtt_connected { get; init; }
    public bool ia_connected { get; init; }
    public Dictionary<string, int> max_length { get; init; }
}
public static class Constants
{
    public const int MaxUrlLength = 150;
    public const int MaxCommentaireLength = 455;
    public const int MaxDescriptionLength = 500;
    public const int MaxNameLength = 50;
    public const int MaxTypeLength = 50;
    public const int MaxEmailLength = 100;
    public const int MaxIpLength = 50;
    public const int MaxReasonLength = 50;
    public const int MaxRoleLength = 50;
    public const int MaxStatusLength = 50;
}