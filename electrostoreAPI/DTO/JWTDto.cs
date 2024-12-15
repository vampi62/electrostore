using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record JWT
{
    public string token { get; init; }
    public DateTime expire_date_token { get; init; }
    public string refesh_token { get; init; }
    public DateTime expire_date_refresh_token { get; init; }
    public Guid token_id { get; init; }
    public Guid refresh_token_id { get; init; }
    public DateTime created_at { get; init; }
}

public record LoginResponse
{
    public string token { get; init; }
    public string expire_date_token { get; init; }
    public string refesh_token { get; init; }
    public string expire_date_refresh_token { get; init; }
    public ReadUserDto user { get; init; }
}

public class JwtSettings
{
    public string Key { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int ExpireDays { get; set; }
}

public record ErrorDetail
{
    public string Reason { get; init; }
    public object Data { get; init; }
}