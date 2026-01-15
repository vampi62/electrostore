using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;


namespace electrostore.Extensions;

public static class VaultConfigurationExtensions
{
    public static IConfigurationBuilder AddVaultConfiguration(this IConfigurationBuilder builder)
    {
        var tempConfig = builder.Build();
        if (tempConfig.GetSection("Vault:Enable").Get<bool>())
        {
            var authMethod = new TokenAuthMethodInfo(tempConfig.GetSection("Vault:Token").Value);
            var vaultConfig = new VaultClientSettings(tempConfig.GetSection("Vault:Addr").Value, authMethod);
            var vaultClient = new VaultClient(vaultConfig);
            // in all config sections, replace values with vault secrets if they are in the format {{vault:<key>}}
            foreach (var section in tempConfig.GetChildren())
            {
                foreach (var child in section.GetChildren())
                {
                    if (child.Value != null && child.Value.StartsWith("{{vault:") && child.Value.EndsWith("}}"))
                    {
                        var key = child.Value[8..^2];
                        var secretValue = GetVaultSecret(vaultClient,
                            tempConfig.GetSection("Vault:Path").Value ?? "secret/electrostore",
                            key
                        );
                        builder.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            { $"{section.Path}:{child.Key}", secretValue }
                        });
                    }
                }
            }
        }

        return builder;
    }
    private static string GetVaultSecret(VaultClient vaultClient, string path, string key)
    {
        var secret = vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path).Result;
        if (secret.Data.Data.TryGetValue(key, out object? value))
        {
            return value?.ToString() ?? string.Empty;
        }
        return string.Empty;
    }
}
