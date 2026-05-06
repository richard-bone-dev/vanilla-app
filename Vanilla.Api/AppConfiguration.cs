using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Vanilla.Api;

public static class AppConfiguration
{
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
        var key = configuration["FIELD_ENCRYPTION_KEY"] ?? configuration["Encryption:FieldKey"];

        if (!string.IsNullOrWhiteSpace(key))
        {
            return key.Trim();
        }

        if (environment.IsEnvironment("Test"))
        {
            return "test-encryption-key";
        }

        throw new InvalidOperationException("FIELD_ENCRYPTION_KEY must be configured outside the Test environment. Legacy Encryption__FieldKey is also supported.");
    }

    public static bool IsApiDocsEnabled(IConfiguration configuration, IHostEnvironment environment) =>
        environment.IsDevelopment() || configuration.GetValue<bool>("ApiDocs:Enabled");

    public static string[] GetAllowedCorsOrigins(IConfiguration configuration) =>
        configuration.GetSection("Cors:AllowedOrigins")
            .Get<string[]>()?
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim())
            .ToArray()
        ?? [];
}
