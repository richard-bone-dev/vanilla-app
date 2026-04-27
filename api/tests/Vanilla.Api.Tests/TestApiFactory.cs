using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Vanilla.Api.Tests;

public sealed class TestApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    private readonly string _connectionString = $"Data Source=VanillaTests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();

        builder.UseEnvironment("Test");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:Sqlite"] = _connectionString,
                ["Encryption:FieldKey"] = "integration-test-key"
            });
        });
    }

    public async Task SeedAsync(Func<AppDbContext, Task> seed)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await seed(dbContext);
        await dbContext.SaveChangesAsync();
    }

    public T Query<T>(Func<AppDbContext, T> query)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return query(dbContext);
    }

    public new async ValueTask DisposeAsync()
    {
        if (_connection is not null) {
            await _connection.DisposeAsync();
        }

        await base.DisposeAsync();
    }
}