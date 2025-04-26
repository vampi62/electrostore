using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadUserDto
{
    public int id_user { get; init; }
    public string nom_user { get; init; }
    public string prenom_user { get; init; }
    public string email_user { get; init; }
    public string role_user { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedUserDto : ReadUserDto
{
    public int projets_commentaires_count { get; init; }
    public int commands_commentaires_count { get; init; }
    public IEnumerable<ReadProjetCommentaireDto>? projets_commentaires { get; init; }
    public IEnumerable<ReadCommandCommentaireDto>? commands_commentaires { get; init; }
}
public record CreateUserDto : IValidatableObject
{
    [Required]
    [MinLength(1, ErrorMessage = "nom_user cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "nom_user cannot exceed 50 characters")]
    public string nom_user { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "prenom_user cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "prenom_user cannot exceed 50 characters")]
    public string prenom_user { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "email_user cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxEmailLength, ErrorMessage = "email_user cannot exceed 100 characters")]
    public string email_user { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "mdp_user cannot be empty or whitespace.")]
    public string mdp_user { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "role_user cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxRoleLength, ErrorMessage = "role_user cannot exceed 50 characters")]
    public string role_user { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(nom_user))
        {
            yield return new ValidationResult("nom_user cannot be null, empty, or whitespace.", new[] { nameof(nom_user) });
        }
        if (string.IsNullOrWhiteSpace(prenom_user))
        {
            yield return new ValidationResult("prenom_user cannot be null, empty, or whitespace.", new[] { nameof(prenom_user) });
        }
        if (string.IsNullOrWhiteSpace(email_user))
        {
            yield return new ValidationResult("email_user cannot be null, empty, or whitespace.", new[] { nameof(email_user) });
        }
        else if (!new EmailAddressAttribute().IsValid(email_user))
        {
            throw new InvalidOperationException("email_user has an Invalid email format");
        }
        if (string.IsNullOrWhiteSpace(role_user))
        {
            yield return new ValidationResult("role_user cannot be null, empty, or whitespace.", new[] { nameof(role_user) });
        }
        if (!new RegularExpressionAttribute(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$").IsValid(mdp_user))
        {
            yield return new ValidationResult("mdp_user must contain a number and a special character and a uppercase letter and a lowercase letter and if it's at least 8 characters long");
        }
    }
}
public record UpdateUserDto : IValidatableObject
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "nom_user cannot exceed 50 characters")]
    public string? nom_user { get; init; }

    [MaxLength(Constants.MaxNameLength, ErrorMessage = "prenom_user cannot exceed 50 characters")]
    public string? prenom_user { get; init; }

    [MaxLength(Constants.MaxEmailLength, ErrorMessage = "email_user cannot exceed 100 characters")]
    public string? email_user { get; init; }

    public string? mdp_user { get; init; }

    [MaxLength(Constants.MaxRoleLength, ErrorMessage = "role_user cannot exceed 50 characters")]
    public string? role_user { get; init; }

    [Required]
    public string current_mdp_user { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (nom_user is not null && string.IsNullOrWhiteSpace(nom_user))
        {
            yield return new ValidationResult("nom_user cannot be empty or whitespace.", new[] { nameof(nom_user) });
        }
        if (prenom_user is not null && string.IsNullOrWhiteSpace(prenom_user))
        {
            yield return new ValidationResult("prenom_user cannot be empty or whitespace.", new[] { nameof(prenom_user) });
        }
        if (email_user is not null && string.IsNullOrWhiteSpace(email_user))
        {
            yield return new ValidationResult("email_user cannot be empty or whitespace.", new[] { nameof(email_user) });
        }
        else if (email_user is not null && !new EmailAddressAttribute().IsValid(email_user))
        {
            throw new InvalidOperationException("email_user has an Invalid email format");
        }
        if (role_user is not null && string.IsNullOrWhiteSpace(role_user))
        {
            yield return new ValidationResult("role_user cannot be empty or whitespace.", new[] { nameof(role_user) });
        }
        if (mdp_user is not null && !new RegularExpressionAttribute(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$").IsValid(mdp_user))
        {
            yield return new ValidationResult("mdp_user must contain a number and a special character and a uppercase letter and a lowercase letter and if it's at least 8 characters long");
        }
    }
}