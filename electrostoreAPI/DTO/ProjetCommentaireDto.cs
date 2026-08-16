using ElectrostoreAPI.Validators;
using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadProjetCommentaireDto
{
    public int id_project_comment { get; init; }
    public int id_project { get; init; }
    public int? id_user { get; init; }
    public required string content_project_comment { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedProjetCommentaireDto : ReadProjetCommentaireDto
{
    public ReadProjetDto? project { get; init; }
    public ReadUserDto? user { get; init; }
}
public record CreateProjetCommentaireByUserDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_project { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxCommentLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string content_project_comment { get; init; }
}
public record CreateProjetCommentaireByProjetDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxCommentLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string content_project_comment { get; init; }
}
public record CreateProjetCommentaireDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_project { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public required int id_user { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxCommentLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string content_project_comment { get; init; }
}
public record UpdateProjetCommentaireDto
{
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxCommentLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? content_project_comment { get; init; }
}