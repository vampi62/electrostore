using ElectrostoreAPI.Validators;
using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record LoginRequest
{
    [Required(ErrorMessage = "{0} is required.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public required string email { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public required string password { get; set; }
}

public record ForgotPasswordRequest
{
    [Required(ErrorMessage = "{0} is required.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public required string email { get; set; }
}

public record ResetPasswordRequest
{
    [Required(ErrorMessage = "{0} is required.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public required string email { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public required string token { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$",
        ErrorMessage = "{0} must contain a number and a special character and a uppercase letter and a lowercase letter and if it's at least 8 characters long")]
    public required string password { get; set; }
}

public record SsoLoginRequest
{
    [Required(ErrorMessage = "{0} is required.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public required string code { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public required string state { get; set; }
}