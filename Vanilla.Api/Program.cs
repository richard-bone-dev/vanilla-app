using Microsoft.EntityFrameworkCore;
using Vanilla.Api;
using Vanilla.Api.Data;
using Vanilla.Api.Security;
using Vanilla.Api.Services;
using Vanilla.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole();

builder.AddServiceDefaults();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IFieldEncryptionService>(_ =>
    new AesGcmFieldEncryptionService(AppConfiguration.GetRequiredEncryptionKey(builder.Configuration, builder.Environment)));
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
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

    if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Test"))
    {
        options.EnableSensitiveDataLogging();
    }
});
builder.Services.AddScoped<LedgerApplicationService>();

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services);

app.MapLedgerApi();
app.MapDefaultEndpoints();

app.Run();

public partial class Program;
