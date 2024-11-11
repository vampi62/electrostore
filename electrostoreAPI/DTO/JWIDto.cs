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
public record UpdateAccessTokenDto
{
    public string? revoked_reason { get; init; }
}