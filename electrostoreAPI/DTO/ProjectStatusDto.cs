using ElectrostoreAPI.Enums;
using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadProjectStatusDto
{
    public int id_project_status { get; init; }
    public int id_project { get; init; }
    public ProjectStatus status_project { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedProjectStatusDto : ReadProjectStatusDto
{
    public ReadProjectDto? project { get; init; }
}
public record CreateProjectStatusDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int id_project { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, (int)ProjectStatus.Completed, ErrorMessage = "{0} must be a valid ProjectStatus value, between {1} and {2}.")]
    public required ProjectStatus status_project { get; init; }
}
public record UpdateProjectStatusDto
{
    [Range(0, (int)ProjectStatus.Completed, ErrorMessage = "{0} must be a valid ProjectStatus value, between {1} and {2}.")]
    public ProjectStatus? status_project { get; init; }
}