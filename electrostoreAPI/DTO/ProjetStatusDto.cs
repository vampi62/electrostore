using System.ComponentModel.DataAnnotations;
using electrostore.Enums;

namespace electrostore.Dto;

public record ReadProjetStatusDto
{
    public int id_projet_status { get; init; }
    public int id_projet { get; init; }
    public ProjetStatus status_projet { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedProjetStatusDto : ReadProjetStatusDto
{
    public ReadProjetDto? projet { get; init; }
}
public record CreateProjetStatusDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int id_projet { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, (int)ProjetStatus.Completed, ErrorMessage = "{0} must be a valid ProjetStatus value, between {1} and {2}.")]
    public required ProjetStatus status_projet { get; init; }
}
public record UpdateProjetStatusDto
{
    [Range(0, (int)ProjetStatus.Completed, ErrorMessage = "{0} must be a valid ProjetStatus value, between {1} and {2}.")]
    public ProjetStatus? status_projet { get; init; }
}