using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadStoreDto
{
    public int id_store { get; init; }
    public string nom_store { get; init; }
    public int xlength_store { get; init; }
    public int ylength_store { get; init; }
    public string mqtt_name_store { get; init; }
}
public record CreateStoreDto
{
    [Required] public string nom_store { get; init; }
    [Required] public int xlength_store { get; init; }
    [Required] public int ylength_store { get; init; }
    [Required] public string mqtt_name_store { get; init; }
}
public record UpdateStoreDto
{
    public string? nom_store { get; init; }
    public int? xlength_store { get; init; }
    public int? ylength_store { get; init; }
    public string? mqtt_name_store { get; init; }
}