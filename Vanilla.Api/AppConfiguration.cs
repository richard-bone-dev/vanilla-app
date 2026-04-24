using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Vanilla.Api;

public static class AppConfiguration
{
    public static string GetRequiredConnectionString(IConfiguration configuration, string provider)
    {
        var connectionString = string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase)
            ? configuration.GetConnectionString("Sqlite")
            : configuration.GetConnectionString("SqlServer") ?? configuration.GetConnectionString("appdb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"{provider} connection string is required.");
        }

        return connectionString;
    }

    public static string GetRequiredEncryptionKey(IConfiguration configuration, IHostEnvironment environment)
    {
        var key = configuration["Encryption:FieldKey"] ?? configuration["FIELD_ENCRYPTION_KEY"];

        if (!string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        if (environment.IsEnvironment("Test"))
        {
            return "test-encryption-key";
        }

        throw new InvalidOperationException("Encryption__FieldKey or FIELD_ENCRYPTION_KEY must be configured outside the Test environment.");
    }
}
