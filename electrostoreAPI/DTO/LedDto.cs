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
    public required List<ReadLedDto> Valide { get; init; }
    public required List<ErrorDetail> Error { get; init; }
}
public record CreateLedByStoreDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int x_led { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int y_led { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int mqtt_led_id { get; init; }
}
public record CreateLedDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int x_led { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int y_led { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public required int id_store { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int mqtt_led_id { get; init; }
}
public record UpdateLedByStoreDto
{
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? x_led { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? y_led { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? mqtt_led_id { get; init; }
}
public record UpdateLedDto
{
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? x_led { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? y_led { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? mqtt_led_id { get; init; }
}
public record UpdateBulkLedByStoreDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_led { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "x_led must be greater than or equal to 0.")]
    public int? x_led { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "y_led must be greater than or equal to 0.")]
    public int? y_led { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "mqtt_led_id must be greater than or equal to 0.")]
    public int? mqtt_led_id { get; init; }

    public string? status { get; init; } // status field to indicate the new status "delete", "modified", "new"
}
