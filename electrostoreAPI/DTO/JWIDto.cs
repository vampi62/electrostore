using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;


public record ReadAccessTokenDto
{
    public Guid id_jwi_access { get; init; }
    public DateTime expires_at { get; init; }
    public bool is_revoked { get; init; }
    public DateTime created_at { get; init; }
    public string created_by_ip { get; init; }
    public DateTime? revoked_at { get; init; }
    public string? revoked_by_ip { get; init; }
    public string? revoked_reason { get; init; }
    public int id_user { get; init; }
}
public record ReadRefreshTokenDto
{
    public Guid id_jwi_refresh { get; init; }
    public DateTime expires_at { get; init; }
    public bool is_revoked { get; init; }
    public DateTime created_at { get; init; }
    public string created_by_ip { get; init; }
    public DateTime? revoked_at { get; init; }
    public string? revoked_by_ip { get; init; }
    public string? revoked_reason { get; init; }
    public Guid id_jwi_access { get; init; }
}
public record UpdateAccessTokenDto : IValidatableObject
{
    [MaxLength(Constants.MaxReasonLength, ErrorMessage = "revoked_reason cannot exceed 50 characters")]
    public string? revoked_reason { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (revoked_reason is not null && string.IsNullOrWhiteSpace(revoked_reason))
        {
            yield return new ValidationResult("revoked_reason cannot be empty or whitespace.", new[] { nameof(revoked_reason) });
        }
    }
}