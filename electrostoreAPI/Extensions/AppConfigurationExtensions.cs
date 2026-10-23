namespace ElectrostoreAPI.Extensions;

public static class AppConfigurationExtensions
{
    private const string AppLanguageKey = "AppLanguage";
    private const string DefaultAppLanguage = "en";

    public static string GetAppLanguage(this IConfiguration configuration)
    {
        return configuration.GetValue<string>(AppLanguageKey) ?? DefaultAppLanguage;
    }
}
