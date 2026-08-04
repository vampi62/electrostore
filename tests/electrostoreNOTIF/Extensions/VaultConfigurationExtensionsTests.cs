using ElectrostoreNOTIF.Extensions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ElectrostoreNOTIF.Tests.Extensions;

public class VaultConfigurationExtensionsTests
{
    private static IConfigurationBuilder BuildConfigBuilder(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values);
    }

    [Fact]
    public void AddVaultConfiguration_ShouldLeaveConfigurationUnchanged_WhenVaultIsNotEnabled()
    {
        // Arrange
        var builder = BuildConfigBuilder(new Dictionary<string, string?>
        {
            ["Vault:Enable"] = "false",
            ["SomeSetting"] = "original-value"
        });

        // Act
        var result = builder.AddVaultConfiguration().Build();

        // Assert
        Assert.Equal("original-value", result["SomeSetting"]);
    }

    [Fact]
    public void AddVaultConfiguration_ShouldLeaveConfigurationUnchanged_WhenVaultEnableIsMissing()
    {
        // Arrange
        var builder = BuildConfigBuilder(new Dictionary<string, string?>
        {
            ["SomeSetting"] = "original-value"
        });

        // Act
        var result = builder.AddVaultConfiguration().Build();

        // Assert
        Assert.Equal("original-value", result["SomeSetting"]);
    }

    [Fact]
    public void AddVaultConfiguration_ShouldThrowInvalidOperationException_WhenAddrIsMissing()
    {
        // Arrange
        var builder = BuildConfigBuilder(new Dictionary<string, string?>
        {
            ["Vault:Enable"] = "true",
            ["Vault:Token"] = "token",
            ["Vault:Path"] = "secret/path",
            ["Vault:MountPoint"] = "secret"
        });

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddVaultConfiguration());

        // Assert
        Assert.Contains("Vault:Addr", exception.Message);
    }

    [Fact]
    public void AddVaultConfiguration_ShouldThrowInvalidOperationException_WhenTokenIsMissing()
    {
        // Arrange
        var builder = BuildConfigBuilder(new Dictionary<string, string?>
        {
            ["Vault:Enable"] = "true",
            ["Vault:Addr"] = "http://vault.local",
            ["Vault:Path"] = "secret/path",
            ["Vault:MountPoint"] = "secret"
        });

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddVaultConfiguration());

        // Assert
        Assert.Contains("Vault:Token", exception.Message);
    }

    [Fact]
    public void AddVaultConfiguration_ShouldThrowInvalidOperationException_WhenPathIsMissing()
    {
        // Arrange
        var builder = BuildConfigBuilder(new Dictionary<string, string?>
        {
            ["Vault:Enable"] = "true",
            ["Vault:Addr"] = "http://vault.local",
            ["Vault:Token"] = "token",
            ["Vault:MountPoint"] = "secret"
        });

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddVaultConfiguration());

        // Assert
        Assert.Contains("Vault:Path", exception.Message);
    }

    [Fact]
    public void AddVaultConfiguration_ShouldThrowInvalidOperationException_WhenMountPointIsMissing()
    {
        // Arrange
        var builder = BuildConfigBuilder(new Dictionary<string, string?>
        {
            ["Vault:Enable"] = "true",
            ["Vault:Addr"] = "http://vault.local",
            ["Vault:Token"] = "token",
            ["Vault:Path"] = "secret/path"
        });

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddVaultConfiguration());

        // Assert
        Assert.Contains("Vault:MountPoint", exception.Message);
    }

    [Fact]
    public void AddVaultConfiguration_ShouldLeaveConfigurationUnchanged_WhenNoValueReferencesVault()
    {
        // No config value contains a "{{vault:<key>}}" placeholder, so the vault client is created
        // but never actually contacted over the network -- this stays a pure unit test.
        // Arrange
        var builder = BuildConfigBuilder(new Dictionary<string, string?>
        {
            ["Vault:Enable"] = "true",
            ["Vault:Addr"] = "http://vault.local",
            ["Vault:Token"] = "token",
            ["Vault:Path"] = "secret/path",
            ["Vault:MountPoint"] = "secret",
            ["SomeSetting"] = "plain-value"
        });

        // Act
        var result = builder.AddVaultConfiguration().Build();

        // Assert
        Assert.Equal("plain-value", result["SomeSetting"]);
    }

    [Fact]
    public void AddVaultConfiguration_ShouldThrowInvalidOperationException_WhenSecretRetrievalFails()
    {
        // A config value references a vault secret ("{{vault:...}}"), which drives execution into
        // the placeholder-substitution loop and the actual Vault HTTP call. Port 1 has nobody
        // listening, so the connection is refused immediately (no DNS/timeout delay), letting
        // GetVaultSecret's catch-and-wrap branch be exercised deterministically and fast.
        // Note: the value must live under a nested key (e.g. "App:ApiKey") - SearchInConfigBranch
        // only inspects the *children* of each top-level section, so a flat top-level key is
        // never checked for a vault placeholder.
        // Arrange
        var builder = BuildConfigBuilder(new Dictionary<string, string?>
        {
            ["Vault:Enable"] = "true",
            ["Vault:Addr"] = "http://127.0.0.1:1",
            ["Vault:Token"] = "token",
            ["Vault:Path"] = "secret/path",
            ["Vault:MountPoint"] = "secret",
            ["App:ApiKey"] = "prefix-{{vault:api-key}}-suffix"
        });

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddVaultConfiguration());

        // Assert
        Assert.Contains("Failed to retrieve secret from Vault", exception.Message);
        Assert.Contains("api-key", exception.Message);
    }
}
