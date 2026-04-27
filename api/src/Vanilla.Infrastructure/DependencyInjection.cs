using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vanilla.Application;
using Vanilla.Infrastructure.Data;
using Vanilla.Infrastructure.Security;

namespace Vanilla.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IFieldEncryptionService>(_ => new AesGcmFieldEncryptionService(AppConfiguration.GetRequiredEncryptionKey(configuration, environment)));

        services.AddDbContext<AppDbContext>(options =>
        {
            var provider = configuration["Database:Provider"] ?? "SqlServer";
            var connectionString = AppConfiguration.GetRequiredConnectionString(configuration, provider);

            if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(connectionString);
            }
            else
            {
                options.UseSqlServer(connectionString, sqlServerOptions =>
                {
                    sqlServerOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    sqlServerOptions.EnableRetryOnFailure();
                });
            }

            if (environment.IsDevelopment() || environment.IsEnvironment("Test"))
            {
                options.EnableSensitiveDataLogging();
            }
        });

        services.AddScoped<IAppDbContext>(serviceProvider => serviceProvider.GetRequiredService<AppDbContext>());
        return services;
    }
}