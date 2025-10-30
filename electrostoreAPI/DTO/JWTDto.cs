namespace electrostore.Dto;

public record Jwt
{
    public required string token { get; init; }
    public DateTime expire_date_token { get; init; }
    public required string refresh_token { get; init; }
    public DateTime expire_date_refresh_token { get; init; }
    public Guid token_id { get; init; }
    public Guid refresh_token_id { get; init; }
    public DateTime created_at { get; init; }
}

public record LoginResponse
{
    public required string token { get; init; }
    public DateTime expire_date_token { get; init; }
    public required string refresh_token { get; init; }
    public DateTime expire_date_refresh_token { get; init; }
    public required ReadUserDto user { get; init; }
}

public class JwtSettings
{
    public required string Key { get; set; }
    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public int ExpireDays { get; set; }
}

public record ErrorDetail
{
    public required string Reason { get; init; }
    public required object Data { get; init; }
}

public record SsoUrlResponse
{
    public required string AuthUrl { get; init; }
    public required string State { get; init; }
}