using ElectrostoreAPI.Validators;
using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadEquipementCommentDto
{
    public int id_equipement_comment { get; init; }
    public int id_equipement { get; init; }
    public int? id_user { get; init; }
    public required string content_equipement_comment { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedEquipementCommentDto : ReadEquipementCommentDto
{
    public ReadEquipementDto? equipement { get; init; }
    public ReadUserDto? user { get; init; }
}
public record CreateEquipementCommentByEquipementDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxCommentLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string content_equipement_comment { get; init; }
}
public record CreateEquipementCommentByUserDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_equipement { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxCommentLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string content_equipement_comment { get; init; }
}
public record CreateEquipementCommentDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_equipement { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public required int id_user { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxCommentLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string content_equipement_comment { get; init; }
}
public record UpdateEquipementCommentDto
{
    [MaxLength(Constants.MaxCommentLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? content_equipement_comment { get; init; }
}
