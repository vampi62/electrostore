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
            var vaultAddr = tempConfig.GetSection("Vault:Addr").Value ?? throw new InvalidOperationException("Vault:Addr configuration is missing.");
            var vaultToken = tempConfig.GetSection("Vault:Token").Value ?? throw new InvalidOperationException("Vault:Token configuration is missing.");
            var vaultPath = tempConfig.GetSection("Vault:Path").Value ?? throw new InvalidOperationException("Vault:Path configuration is missing.");
            var vaultMountPoint = tempConfig.GetSection("Vault:MountPoint").Value ?? throw new InvalidOperationException("Vault:MountPoint configuration is missing.");
            var authMethod = new TokenAuthMethodInfo(vaultToken);
            var vaultConfig = new VaultClientSettings(vaultAddr, authMethod);
            var vaultClient = new VaultClient(vaultConfig);
            // in all config sections, replace values with vault secrets if they are in the format {{vault:<key>}}
            foreach (var section in tempConfig.GetChildren())
            {
                foreach (var child in section.GetChildren())
                {
                    if (child.Value != null && child.Value.Contains("{{vault:") && child.Value.Contains("}}"))
                    {
                        Console.WriteLine($"Checking key: {child.Key} with value: {child.Value}");
                        // for all occurrences of {{vault:<key>}} in the value, replace with the corresponding vault secret
                        var newValue = child.Value;
                        int startIndex = 0;
                        while (true)
                        {
                            int vaultStart = newValue.IndexOf("{{vault:", startIndex);
                            if (vaultStart == -1) break;
                            int vaultEnd = newValue.IndexOf("}}", vaultStart);
                            if (vaultEnd == -1) break;
                            var vaultKey = newValue.Substring(vaultStart + 8, vaultEnd - vaultStart - 8);
                            var secretValue = GetVaultSecret(vaultClient, vaultPath, vaultMountPoint, vaultKey);
                            newValue = string.Concat(newValue.AsSpan(0, vaultStart), secretValue, newValue.AsSpan(vaultEnd + 2));
                            startIndex = vaultStart + secretValue.Length;
                        }
                        // update the configuration with the new value
                        builder.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            { child.Path, newValue }
                        });
                    }
                }
            }
        }
        return builder;
    }
    private static string GetVaultSecret(VaultClient vaultClient, string path, string mountPoint, string key)
    {
        try
        {
            var secret = vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: path, mountPoint: mountPoint).GetAwaiter().GetResult();
            if (secret.Data.Data.TryGetValue(key, out object? value))
            {
                return value?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading secret from Vault: {ex.Message}");
            Console.WriteLine($"Make sure the KV v2 engine is enabled and the secret exists at path: '{path}'");
            throw new InvalidOperationException($"Failed to retrieve secret from Vault at path '{path}' for key '{key}': {ex.Message}", ex);
        }
    }
}
