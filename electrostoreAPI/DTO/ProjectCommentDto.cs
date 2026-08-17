using ElectrostoreAPI.Validators;
using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadProjectCommentDto
{
    public int id_project_comment { get; init; }
    public int id_project { get; init; }
    public int? id_user { get; init; }
    public required string content_project_comment { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedProjectCommentDto : ReadProjectCommentDto
{
    public ReadProjectDto? project { get; init; }
    public ReadUserDto? user { get; init; }
}
public record CreateProjectCommentByUserDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_project { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxCommentLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string content_project_comment { get; init; }
}
public record CreateProjectCommentByProjectDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxCommentLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string content_project_comment { get; init; }
}
public record CreateProjectCommentDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_project { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public required int id_user { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxCommentLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string content_project_comment { get; init; }
}
public record UpdateProjectCommentDto
{
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxCommentLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? content_project_comment { get; init; }
}