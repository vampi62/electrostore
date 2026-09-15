namespace ElectrostoreAPI.Dto;

public record ReadConfig
{
    public bool demo_mode { get; init; }
    public string app_language { get; init; } = "en";
    public int max_length_url { get; init; }
    public int max_length_comment { get; init; }
    public int max_length_description { get; init; }
    public int max_length_name { get; init; }
    public int max_length_type { get; init; }
    public int max_length_email { get; init; }
    public int max_length_location { get; init; }
    public int max_length_cron_expression { get; init; }
    public int max_length_ip { get; init; }
    public int max_length_reason { get; init; }
    public int max_length_status { get; init; }
    public int max_length_device_name { get; init; }
    public int max_length_push_key { get; init; }
    public int max_length_push_auth { get; init; }
    public int max_length_tracking_number { get; init; }
    public int max_length_carrier_name { get; init; }
    public int max_length_timezone { get; init; }
    public int max_length_coordinate { get; init; }
    public int max_length_postal_code { get; init; }
    public int max_size_document_in_mb { get; init; }
    public int max_size_image_in_mb { get; init; }
    public List<SsoAvailableProvider>? sso_available_providers { get; init; }
    public string[]? allowed_image_mime_types { get; init; }
    public string[]? allowed_image_extensions { get; init; }
    public string[]? allowed_document_mime_types { get; init; }
    public string[]? allowed_document_extensions { get; init; }
}

public record SsoAvailableProvider
{
    public required string provider { get; init; }
    public required string display_name { get; init; }
    public required string icon_url { get; init; }
}