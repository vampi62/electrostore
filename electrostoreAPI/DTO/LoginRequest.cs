using System.ComponentModel.DataAnnotations;
using electrostore.Validators;

namespace electrostore.Dto;

public record LoginRequest
{
    [Required(ErrorMessage = "{0} is required.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? Password { get; set; }
}

public record ForgotPasswordRequest
{
    [Required(ErrorMessage = "{0} is required.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? Email { get; set; }
}

public record ResetPasswordRequest
{
    [Required(ErrorMessage = "{0} is required.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? Token { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$",
        ErrorMessage = "{0} must contain a number and a special character and a uppercase letter and a lowercase letter and if it's at least 8 characters long")]
    public string? Password { get; set; }
}

public record SsoLoginRequest
{
    [Required(ErrorMessage = "{0} is required.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? Code { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? State { get; set; }
}