using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Validators;
using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadProjectStepDto
{
    public int id_project_step { get; init; }
    public int id_project { get; init; }
    public required string name_project_step { get; init; }
    public string description_project_step { get; init; } = string.Empty;
    public ProjectStepStatus status_project_step { get; init; }
    public int order_project_step { get; init; }
    public DateTime? planned_start_project_step { get; init; }
    public DateTime? planned_end_project_step { get; init; }
    public DateTime? actual_start_project_step { get; init; }
    public DateTime? actual_end_project_step { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedProjectStepDto : ReadProjectStepDto
{
    public ReadProjectDto? project { get; init; }
}
public record CreateProjectStepByProjectDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string name_project_step { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? description_project_step { get; init; }

    [Range(0, (int)ProjectStepStatus.Cancelled, ErrorMessage = "{0} must be a valid status, between {1} and {2}.")]
    public ProjectStepStatus? status_project_step { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}.")]
    public int? order_project_step { get; init; }

    public DateTime? planned_start_project_step { get; init; }

    public DateTime? planned_end_project_step { get; init; }

    public DateTime? actual_start_project_step { get; init; }

    public DateTime? actual_end_project_step { get; init; }
}
public record CreateProjectStepDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_project { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string name_project_step { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? description_project_step { get; init; }

    [Range(0, (int)ProjectStepStatus.Cancelled, ErrorMessage = "{0} must be a valid status, between {1} and {2}.")]
    public ProjectStepStatus? status_project_step { get; init; }

    public int order_project_step { get; init; }

    public DateTime? planned_start_project_step { get; init; }

    public DateTime? planned_end_project_step { get; init; }

    public DateTime? actual_start_project_step { get; init; }

    public DateTime? actual_end_project_step { get; init; }
}
public record UpdateProjectStepDto
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? name_project_step { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? description_project_step { get; init; }

    [Range(0, (int)ProjectStepStatus.Cancelled, ErrorMessage = "{0} must be a valid status, between {1} and {2}.")]
    public ProjectStepStatus? status_project_step { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}.")]
    public int? order_project_step { get; init; }

    public DateTime? planned_start_project_step { get; init; }

    public DateTime? planned_end_project_step { get; init; }

    public DateTime? actual_start_project_step { get; init; }

    public DateTime? actual_end_project_step { get; init; }
}
