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
    public required List<ReadProjetItemDto> Valide { get; init; }
    public required List<ErrorDetail> Error { get; init; }
}
public record CreateProjetItemByProjetDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_item { get; init; }
    
    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int qte_projet_item { get; init; }
}
public record CreateProjetItemByItemDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_projet { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int qte_projet_item { get; init; }
}
public record CreateProjetItemDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_projet { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public required int id_item { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int qte_projet_item { get; init; }
}
public record UpdateProjetItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? qte_projet_item { get; init; }
}