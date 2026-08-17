using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadProjectProjectTagDto
{
    public int id_project { get; init; }
    public int id_project_tag { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedProjectProjectTagDto : ReadProjectProjectTagDto
{
    public ReadProjectTagDto? project_tag { get; init; }
    public ReadProjectDto? project { get; init; }
}
public record ReadBulkProjectProjectTagDto
{
    public required List<ReadProjectProjectTagDto> Valide { get; init; }
    public required List<ErrorDetail> Error { get; init; }
}
public record CreateProjectProjectTagByProjectTagDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_project { get; init; }
}
public record CreateProjectProjectTagByProjectDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_project_tag { get; init; }
}
public record CreateProjectProjectTagDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_project { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public required int id_project_tag { get; init; }
}