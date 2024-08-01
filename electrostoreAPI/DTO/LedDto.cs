using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadLedDto
{
    public int id_led { get; init; }
    public int x_led { get; init; }
    public int y_led { get; init; }
    public int id_store { get; init; }
    public int mqtt_led_id { get; init; }
}
public record CreateLedByStoreDto
{
    [Required] public int x_led { get; init; }
    [Required] public int y_led { get; init; }
    [Required] public int mqtt_led_id { get; init; }
}
public record CreateLedDto
{
    [Required] public int x_led { get; init; }
    [Required] public int y_led { get; init; }
    [Required] public int id_store { get; init; }
    [Required] public int mqtt_led_id { get; init; }
}
public record UpdateLedByStoreDto
{
    public int? x_led { get; init; }
    public int? y_led { get; init; }
    public int? mqtt_led_id { get; init; }
}
public record UpdateLedDto
{
    public int? x_led { get; init; }
    public int? y_led { get; init; }
    public int? new_id_store { get; init; }
    public int? mqtt_led_id { get; init; }
}