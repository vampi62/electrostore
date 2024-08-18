using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public class LoginRequest
{
    [Required] public string Email { get; set; }

    [Required] public string Password { get; set; }
}

public class ForgotPasswordRequest
{
    [Required] public string Email { get; set; }
}

public class ResetPasswordRequest
{
    [Required] public string Email { get; set; }

    [Required] public string Token { get; set; }

    [Required] public string Password { get; set; }
}