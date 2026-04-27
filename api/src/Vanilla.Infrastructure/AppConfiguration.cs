using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Vanilla.Infrastructure;

public static class AppConfiguration
{
    private const string TestEncryptionKey = "test-encryption-key";

    public static string GetRequiredConnectionString(IConfiguration configuration, string provider)
    {
        var connectionString = string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase)
            ? configuration.GetConnectionString("Sqlite")
            : configuration.GetConnectionString("DefaultConnection")
                ?? configuration.GetConnectionString("SqlServer")
                ?? configuration.GetConnectionString("appdb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"{provider} connection string is required.");
        }

        return connectionString;
    }

    public static string GetRequiredEncryptionKey(IConfiguration configuration, IHostEnvironment environment)
    {
        var key = GetConfiguredEncryptionKey(configuration);

        if (!string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        if (environment.IsEnvironment("Test"))
        {
            return TestEncryptionKey;
        }

        throw new InvalidOperationException(
            "An encryption key is required outside the Test environment. Configure Encryption:FieldKey, Encryption__FieldKey, or FIELD_ENCRYPTION_KEY.");
    }

    public static string? GetConfiguredEncryptionKey(IConfiguration configuration)
    {
        return configuration["Encryption:FieldKey"]
            ?? configuration["Encryption__FieldKey"]
            ?? configuration["FIELD_ENCRYPTION_KEY"];
    }
}
