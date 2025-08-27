using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record LoginRequest : IValidatableObject
{
    [Required]
    [MinLength(1, ErrorMessage = "Email cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxEmailLength, ErrorMessage = "Email cannot exceed 100 characters")]
    public required string Email { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Password cannot be empty or whitespace.")]
    public required string Password { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            yield return new ValidationResult("Email cannot be null, empty, or whitespace.", new[] { nameof(Email) });
        }
        if (string.IsNullOrWhiteSpace(Password))
        {
            yield return new ValidationResult("Password cannot be null, empty, or whitespace.", new[] { nameof(Password) });
        }
    }
}

public record ForgotPasswordRequest : IValidatableObject
{
    [Required]
    [MinLength(1, ErrorMessage = "Email cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxEmailLength, ErrorMessage = "Email cannot exceed 100 characters")]
    public required string Email { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            yield return new ValidationResult("Email cannot be null, empty, or whitespace.", new[] { nameof(Email) });
        }
    }
}

public record ResetPasswordRequest : IValidatableObject
{
    [Required]
    [MinLength(1, ErrorMessage = "Email cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxEmailLength, ErrorMessage = "Email cannot exceed 100 characters")]
    public required string Email { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Token cannot be empty or whitespace.")]
    [MaxLength(50, ErrorMessage = "Token cannot exceed 50 characters")]
    public required string Token { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Password cannot be empty or whitespace.")]
    public required string Password { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            yield return new ValidationResult("Email cannot be null, empty, or whitespace.", new[] { nameof(Email) });
        }
        if (string.IsNullOrWhiteSpace(Token))
        {
            yield return new ValidationResult("Token cannot be null, empty, or whitespace.", new[] { nameof(Token) });
        }
        if (!new RegularExpressionAttribute(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$").IsValid(Password))
        {
            yield return new ValidationResult("Password must contain a number and a special character and a uppercase letter and a lowercase letter and if it's at least 8 characters long");
        }
    }
}