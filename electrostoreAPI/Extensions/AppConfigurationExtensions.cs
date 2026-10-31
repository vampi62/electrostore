namespace ElectrostoreAPI.Extensions;

public static class AppConfigurationExtensions
{
    private const string AppLanguageKey = "AppLanguage";
    private const string DefaultAppLanguage = "en";

    // the notifications are sent unless SMTP is explicitly disabled
    public static bool IsSmtpEnabled(this IConfiguration configuration)
    {
        return !(bool.TryParse(configuration["SMTP:Enable"], out var isEnabled) && !isEnabled);
    }

    public static string GetAppLanguage(this IConfiguration configuration)
    {
        return configuration.GetValue<string>(AppLanguageKey) ?? DefaultAppLanguage;
    }
}
