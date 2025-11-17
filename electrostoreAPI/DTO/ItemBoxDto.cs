using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadItemBoxDto
{
    public int id_box { get; init; }
    public int id_item { get; init; }
    public int qte_item_box { get; init; }
    public int seuil_max_item_item_box { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedItemBoxDto : ReadItemBoxDto
{
    public ReadItemDto? item { get; init; }
    public ReadBoxDto? box { get; init; }
}
public record CreateItemBoxByBoxDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_item { get; init; }
    
    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int qte_item_box { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int seuil_max_item_item_box { get; init; }
}
public record CreateItemBoxByItemDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_box { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int qte_item_box { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int seuil_max_item_item_box { get; init; }
}
public record CreateItemBoxDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_box { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public required int id_item { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int qte_item_box { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int seuil_max_item_item_box { get; init; }
}
public record UpdateItemBoxDto
{
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? qte_item_box { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? seuil_max_item_item_box { get; init; }
}