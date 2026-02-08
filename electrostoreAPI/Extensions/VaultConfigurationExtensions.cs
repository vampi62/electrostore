using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;


namespace electrostore.Extensions;

interface vaultConfiguration
{
    public string Addr { get; set; }
    public string Token { get; set; }
    public string Path { get; set; }
    public string MountPoint { get; set; }
}

class VaultConfigurationImpl : vaultConfiguration
{
    public required string Addr { get; set; }
    public required string Token { get; set; }
    public required string Path { get; set; }
    public required string MountPoint { get; set; }
}

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
                SearchInConfigBranch(builder, vaultClient,
                new VaultConfigurationImpl
                {
                    Addr = vaultAddr,
                    Token = vaultToken,
                    Path = vaultPath,
                    MountPoint = vaultMountPoint
                }, section);
            }
        }
        return builder;
    }

    private static void SearchInConfigBranch(IConfigurationBuilder builder, VaultClient vaultClient, vaultConfiguration vaultConfig, IConfigurationSection section)
    {
        foreach (var child in section.GetChildren())
        {
            // if the section has a value, check if it contains a vault reference and replace it with the secret value
            if (child.Value != null)            {
                FindSecretInConfigValue(builder, vaultClient, vaultConfig, child);
            }
            else
            {
                // if the section has no value, it might be a branch, so we need to search in its children
                SearchInConfigBranch(builder, vaultClient, vaultConfig, child);
            }
        }
    }

    private static void FindSecretInConfigValue(IConfigurationBuilder builder, VaultClient vaultClient, vaultConfiguration vaultConfig, IConfigurationSection section)
    {
        if (section.Value != null && section.Value.Contains("{{vault:") && section.Value.Contains("}}"))
        {
            Console.WriteLine($"Checking key: {section.Key} with value: {section.Value}");
            // for all occurrences of {{vault:<key>}} in the value, replace with the corresponding vault secret
            var newValue = section.Value;
            int startIndex = 0;
            while (true)
            {
                int vaultStart = newValue.IndexOf("{{vault:", startIndex);
                if (vaultStart == -1) break;
                int vaultEnd = newValue.IndexOf("}}", vaultStart);
                if (vaultEnd == -1) break;
                var vaultKey = newValue.Substring(vaultStart + 8, vaultEnd - vaultStart - 8);
                var secretValue = GetVaultSecret(vaultClient, vaultConfig.Path, vaultConfig.MountPoint, vaultKey);
                newValue = string.Concat(newValue.AsSpan(0, vaultStart), secretValue, newValue.AsSpan(vaultEnd + 2));
                startIndex = vaultStart + secretValue.Length;
            }
            builder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { section.Path, newValue }
            });
            Console.WriteLine($"Updated key: {section.Key} with new value: {newValue}");
        }
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
