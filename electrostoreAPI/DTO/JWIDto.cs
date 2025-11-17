using System.ComponentModel.DataAnnotations;
using electrostore.Validators;

namespace electrostore.Dto;


public record ReadAccessTokenDto
{
    public Guid id_jwi_access { get; init; }
    public Guid session_id { get; init; }
    public DateTime expires_at { get; init; }
    public required string auth_method { get; init; }
    public bool is_revoked { get; init; }
    public DateTime created_at { get; init; }
    public required string created_by_ip { get; init; }
    public DateTime? revoked_at { get; init; }
    public string? revoked_by_ip { get; init; }
    public string? revoked_reason { get; init; }
    public int id_user { get; init; }
}
public record ReadRefreshTokenDto
{
    public Guid id_jwi_refresh { get; init; }
    public Guid session_id { get; init; }
    public DateTime expires_at { get; init; }
    public required string auth_method { get; init; }
    public bool is_revoked { get; init; }
    public DateTime created_at { get; init; }
    public required string created_by_ip { get; init; }
    public DateTime? revoked_at { get; init; }
    public string? revoked_by_ip { get; init; }
    public string? revoked_reason { get; init; }
    public Guid id_jwi_access { get; init; }
    public int id_user { get; init; }
}
public record SessionDto
{
    public Guid session_id { get; init; }
    public DateTime expires_at { get; init; }
    public bool is_revoked { get; init; }
    public required string auth_method { get; init; }
    public DateTime created_at { get; init; }
    public required string created_by_ip { get; init; }
    public DateTime? revoked_at { get; init; }
    public string? revoked_by_ip { get; init; }
    public string? revoked_reason { get; init; }
    public int id_user { get; init; }
    public DateTime first_created_at { get; init; }
}
public record UpdateAccessTokenDto
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? revoked_reason { get; init; }
}