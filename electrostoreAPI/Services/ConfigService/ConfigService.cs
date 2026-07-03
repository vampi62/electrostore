using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.ConfigService;

public class ConfigService : IConfigService
{
    private readonly IConfiguration _configuration;

    public ConfigService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<ReadConfig> getAllConfig()
    {
        return new ReadConfig
        {
            demo_mode = GetDemoMode(),
            app_language = GetAppLanguage(),
            // get the max length of the url
            max_length_url = Constants.MaxUrlLength,
            // get the max length
            max_length_commentaire = Constants.MaxCommentaireLength,
            max_length_description = Constants.MaxDescriptionLength,
            max_length_name = Constants.MaxNameLength,
            max_length_type = Constants.MaxTypeLength,
            max_length_email = Constants.MaxEmailLength,
            max_length_ip = Constants.MaxIpLength,
            max_length_reason = Constants.MaxReasonLength,
            max_length_status = Constants.MaxStatusLength,
            max_size_document_in_mb = Constants.MaxDocumentSizeMB,
            sso_available_providers = GetSSOProviders(),
            allowed_image_mime_types = GetAllowedImageMimeTypes(),
            allowed_image_extensions = GetAllowedImageExtensions(),
            allowed_document_mime_types = GetAllowedDocumentMimeTypes(),
            allowed_document_extensions = GetAllowedDocumentExtensions()
        };
    }

    public bool GetDemoMode() => _configuration.GetValue<bool>("DemoMode");

    public string GetAppLanguage() => _configuration.GetValue<string>("AppLanguage") ?? "fr";

    public string[] GetAllowedImageExtensions() => [.. Constants.AllowedImageMimeTypes.Values];

    private string[] GetAllowedImageMimeTypes() => [.. Constants.AllowedImageMimeTypes.Keys];

    private string[] GetAllowedDocumentExtensions() => [.. Constants.AllowedDocumentMimeTypes.Values];

    private string[] GetAllowedDocumentMimeTypes() => [.. Constants.AllowedDocumentMimeTypes.Keys];

    private List<SSOAvailableProvider> GetSSOProviders() =>
        [.. _configuration.GetSection("OAuth").GetChildren().Select(provider => new SSOAvailableProvider
        {
            provider = provider.Key,
            display_name = provider.GetValue<string>("DisplayName") ?? string.Empty,
            icon_url = provider.GetValue<string>("IconUrl") ?? string.Empty
        })];
}