using ElectrostoreAPI.Enums;
using System.Text.Json.Serialization;

namespace ElectrostoreAPI.Dto;

public record ReadCarrierDto
{
    public int id_carrier { get; init; }
    public int key_carrier { get; init; }
    public int? country_carrier { get; init; }
    public string? country_iso_carrier { get; init; }
    public string? email_carrier { get; init; }
    public string? tel_carrier { get; init; }
    public string? url_carrier { get; init; }
    public string? name_carrier { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}

public record CreateCarrierDto
{
    public int key_carrier { get; init; }
    public int? country_carrier { get; init; }
    public string? country_iso_carrier { get; init; }
    public string? email_carrier { get; init; }
    public string? tel_carrier { get; init; }
    public string? url_carrier { get; init; }
    public string? name_carrier { get; init; }
}

public record UpdateCarrierDto
{
    public int? country_carrier { get; init; }
    public string? country_iso_carrier { get; init; }
    public string? email_carrier { get; init; }
    public string? tel_carrier { get; init; }
    public string? url_carrier { get; init; }
    public string? name_carrier { get; init; }
}

public record JsonCarrierDto
{
    public int key { get; init; }
    public int? _country { get; init; }
    public string? _country_iso { get; init; }
    public string? _email { get; init; }
    public string? _tel { get; init; }
    public string? _url { get; init; }
    public string? _name { get; init; }

    [JsonPropertyName("_name_zh-cn")]
    public string? _name_zh_cn { get; init; }

    [JsonPropertyName("_name_zh-hk")]
    public string? _name_zh_hk { get; init; }
}