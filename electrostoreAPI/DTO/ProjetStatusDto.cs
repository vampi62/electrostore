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
    [Required]
    public required int id_projet { get; init; }

    [Required]
    public ProjetStatus status_projet { get; init; }
}
public record UpdateProjetStatusDto
{
    public ProjetStatus? status_projet { get; init; }
}