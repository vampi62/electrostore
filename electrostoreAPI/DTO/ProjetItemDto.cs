using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadProjetItemDto
{
    public int id_projet { get; init; }
    public int id_item { get; init; }
    public int qte_projet_item { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedProjetItemDto : ReadProjetItemDto
{
    public ReadItemDto? item { get; init; }
    public ReadProjetDto? projet { get; init; }
}
public record ReadBulkProjetItemDto
{
    public List<ReadProjetItemDto> Valide { get; init; }
    public List<ErrorDetail> Error { get; init; }
}
public record CreateProjetItemByProjetDto
{
    [Required]
    public required int id_item { get; init; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "qte_projet_item must be greater than 0.")]
    public required int qte_projet_item { get; init; }
}
public record CreateProjetItemByItemDto
{
    [Required]
    public required int id_projet { get; init; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "qte_projet_item must be greater than 0.")]
    public required int qte_projet_item { get; init; }
}
public record CreateProjetItemDto
{
    [Required]
    public required int id_projet { get; init; }

    [Required]
    public required int id_item { get; init; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "qte_projet_item must be greater than 0.")]
    public required int qte_projet_item { get; init; }
}
public record UpdateProjetItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "qte_projet_item must be greater than 0.")]
    public int? qte_projet_item { get; init; }
}