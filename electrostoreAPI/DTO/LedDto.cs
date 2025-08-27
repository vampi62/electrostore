using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadLedDto
{
    public int id_led { get; init; }
    public int x_led { get; init; }
    public int y_led { get; init; }
    public int id_store { get; init; }
    public int mqtt_led_id { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadBulkLedDto
{
    public List<ReadLedDto> Valide { get; init; }
    public List<ErrorDetail> Error { get; init; }
}
public record CreateLedByStoreDto
{
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "x_led must be greater than or equal to 0.")]
    public required int x_led { get; init; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "y_led must be greater than or equal to 0.")]
    public required int y_led { get; init; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "mqtt_led_id must be greater than or equal to 0.")]
    public required int mqtt_led_id { get; init; }
}
public record CreateLedDto
{
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "x_led must be greater than or equal to 0.")]
    public required int x_led { get; init; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "y_led must be greater than or equal to 0.")]
    public required int y_led { get; init; }

    [Required]
    public required int id_store { get; init; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "mqtt_led_id must be greater than or equal to 0.")]
    public required int mqtt_led_id { get; init; }
}
public record UpdateLedByStoreDto
{
    [Range(0, int.MaxValue, ErrorMessage = "x_led must be greater than or equal to 0.")]
    public int? x_led { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "y_led must be greater than or equal to 0.")]
    public int? y_led { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "mqtt_led_id must be greater than or equal to 0.")]
    public int? mqtt_led_id { get; init; }
}
public record UpdateLedDto
{
    [Range(0, int.MaxValue, ErrorMessage = "x_led must be greater than or equal to 0.")]
    public int? x_led { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "y_led must be greater than or equal to 0.")]
    public int? y_led { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "mqtt_led_id must be greater than or equal to 0.")]
    public int? mqtt_led_id { get; init; }
}
public record UpdateBulkLedByStoreDto
{
    public int id_led { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "x_led must be greater than or equal to 0.")]
    public int? x_led { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "y_led must be greater than or equal to 0.")]
    public int? y_led { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "mqtt_led_id must be greater than or equal to 0.")]
    public int? mqtt_led_id { get; init; }

    public string? status { get; init; } // status field to indicate the new status "delete", "modified", "new"
}
