using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Validators;
using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadUserDto
{
    public int id_user { get; init; }
    public required string name_user { get; init; }
    public required string firstname_user { get; init; }
    public required string email_user { get; init; }
    public UserRole role_user { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedUserDto : ReadUserDto
{
    public int project_comments_count { get; init; }
    public int command_comments_count { get; init; }
    public IEnumerable<ReadProjectCommentDto>? project_comments { get; init; }
    public IEnumerable<ReadCommandCommentDto>? command_comments { get; init; }
}
public record CreateUserDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters")]
    public required string name_user { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters")]
    public required string firstname_user { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxEmailLength, ErrorMessage = "{0} cannot exceed {1} characters")]
    [EmailAddress(ErrorMessage = "{0} must be a valid email address.")]
    public required string email_user { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$",
        ErrorMessage = "{0} must contain a number and a special character and a uppercase letter and a lowercase letter and if it's at least 8 characters long")]
    public required string password_user { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, (int)UserRole.Admin, ErrorMessage = "{0} must be a valid role, between {1} and {2}.")]
    public required UserRole role_user { get; init; }
}
public record UpdateUserDto
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? name_user { get; init; }

    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? firstname_user { get; init; }

    [MaxLength(Constants.MaxEmailLength, ErrorMessage = "{0} cannot exceed {1} characters")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    [EmailAddress(ErrorMessage = "{0} must be a valid email address.")]
    public string? email_user { get; init; }

    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$",
        ErrorMessage = "password_user must contain a number and a special character and a uppercase letter and a lowercase letter and if it's at least 8 characters long")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? password_user { get; init; }

    [Range(0, (int)UserRole.Admin, ErrorMessage = "{0} must be a valid role, between {1} and {2}.")]
    public UserRole? role_user { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public string? current_password_user { get; init; }
}