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
    [Required]
    public int id_item { get; init; }
    
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "qte_item_box must be greater than or equal to 0.")]
    public int qte_item_box { get; init; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "seuil_max_item_item_box must be greater than 0.")]
    public int seuil_max_item_item_box { get; init; }
}
public record CreateItemBoxByItemDto
{
    [Required]
    public int id_box { get; init; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "qte_item_box must be greater than or equal to 0.")]
    public int qte_item_box { get; init; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "seuil_max_item_item_box must be greater than 0.")]
    public int seuil_max_item_item_box { get; init; }
}
public record CreateItemBoxDto
{
    [Required]
    public int id_box { get; init; }

    [Required]
    public int id_item { get; init; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "qte_item_box must be greater than or equal to 0.")]
    public int qte_item_box { get; init; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "seuil_max_item_item_box must be greater than 0.")]
    public int seuil_max_item_item_box { get; init; }
}
public record UpdateItemBoxDto
{
    [Range(0, int.MaxValue, ErrorMessage = "qte_item_box must be greater than or equal to 0.")]
    public int? qte_item_box { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "seuil_max_item_item_box must be greater than 0.")]
    public int? seuil_max_item_item_box { get; init; }
}