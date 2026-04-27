using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Vanilla.Api.Tests;

public sealed class ConfigurationAndEncryptionTests
{
    [Fact]
    public void Test_environment_uses_deterministic_key_when_external_key_is_missing()
    {
        var configuration = new ConfigurationBuilder().Build();
        var environment = new FakeHostEnvironment("Test");

        var key = AppConfiguration.GetRequiredEncryptionKey(configuration, environment);

        key.Should().Be("test-encryption-key");
    }

    [Fact]
    public void Connection_string_can_be_read_from_config()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=sqlserver,1433;Database=VanillaApp;"
        }).Build();

        AppConfiguration.GetRequiredConnectionString(configuration, "SqlServer").Should().Be("Server=sqlserver,1433;Database=VanillaApp;");
    }

    [Fact]
    public void Encryption_key_must_be_present_outside_test_mode()
    {
        var configuration = new ConfigurationBuilder().Build();
        var environment = new FakeHostEnvironment("Development");
        var action = () => AppConfiguration.GetRequiredEncryptionKey(configuration, environment);
        action.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Encryption:FieldKey*Encryption__FieldKey*FIELD_ENCRYPTION_KEY*");
    }

    [Fact]
    public void Encryption_key_can_be_read_from_hierarchical_configuration_key()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Encryption:FieldKey"] = "configured-key"
        }).Build();
        var environment = new FakeHostEnvironment("Development");

        var key = AppConfiguration.GetRequiredEncryptionKey(configuration, environment);

        key.Should().Be("configured-key");
    }

    [Fact]
    public void Encryption_key_can_be_read_from_compatibility_fallback_key()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["FIELD_ENCRYPTION_KEY"] = "fallback-key"
        }).Build();
        var environment = new FakeHostEnvironment("Development");

        var key = AppConfiguration.GetRequiredEncryptionKey(configuration, environment);

        key.Should().Be("fallback-key");
    }

    [Fact]
    public void Encryption_key_can_be_read_from_double_underscore_key()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Encryption__FieldKey"] = "env-style-key"
        }).Build();
        var environment = new FakeHostEnvironment("Development");

        var key = AppConfiguration.GetRequiredEncryptionKey(configuration, environment);

        key.Should().Be("env-style-key");
    }

    [Fact]
    public async Task Encrypted_fields_are_not_stored_as_plaintext()
    {
        await using var dbContext = await CreateDbContextAsync();
        dbContext.Customers.Add(new Customer { Name = "Alice", Email = "alice@example.com", Phone = "12345", Notes = "private note" });
        await dbContext.SaveChangesAsync();
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT Email, Phone, Notes FROM Customers LIMIT 1;";
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        reader.GetString(0).Should().NotBe("alice@example.com");
        reader.GetString(1).Should().NotBe("12345");
        reader.GetString(2).Should().NotBe("private note");
    }

    [Fact]
    public async Task Customer_email_encrypts_and_decrypts()
    {
        await using var dbContext = await CreateDbContextAsync();
        dbContext.Customers.Add(new Customer { Name = "Alice", Email = "alice@example.com" });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        (await dbContext.Customers.SingleAsync()).Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task Customer_phone_encrypts_and_decrypts()
    {
        await using var dbContext = await CreateDbContextAsync();
        dbContext.Customers.Add(new Customer { Name = "Alice", Phone = "12345" });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        (await dbContext.Customers.SingleAsync()).Phone.Should().Be("12345");
    }

    [Fact]
    public async Task Customer_notes_encrypt_and_decrypt()
    {
        await using var dbContext = await CreateDbContextAsync();
        dbContext.Customers.Add(new Customer { Name = "Alice", Notes = "customer note" });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        (await dbContext.Customers.SingleAsync()).Notes.Should().Be("customer note");
    }

    [Fact]
    public async Task Order_notes_encrypt_and_decrypt()
    {
        await using var dbContext = await CreateDbContextAsync();
        var customer = new Customer { Name = "Alice" };
        dbContext.Customers.Add(customer);
        dbContext.Orders.Add(new Order { Customer = customer, Amount = 25m, Notes = "order note", CreatedUtc = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        (await dbContext.Orders.SingleAsync()).Notes.Should().Be("order note");
    }

    [Fact]
    public async Task Payment_notes_encrypt_and_decrypt()
    {
        await using var dbContext = await CreateDbContextAsync();
        var customer = new Customer { Name = "Alice" };
        dbContext.Customers.Add(customer);
        dbContext.Payments.Add(new Payment { Customer = customer, Amount = 10m, Notes = "payment note", CreatedUtc = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        (await dbContext.Payments.SingleAsync()).Notes.Should().Be("payment note");
    }

    [Fact]
    public async Task Amount_remains_queryable_as_decimal()
    {
        await using var dbContext = await CreateDbContextAsync();
        var customer = new Customer { Name = "Alice" };
        dbContext.Customers.Add(customer);
        dbContext.Orders.AddRange(new Order { Customer = customer, Amount = 12.50m, CreatedUtc = DateTime.UtcNow }, new Order { Customer = customer, Amount = 7.50m, CreatedUtc = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();
        (await dbContext.Orders.SumAsync(order => order.Amount)).Should().Be(20m);
    }

    private static async Task<AppDbContext> CreateDbContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var dbContext = new AppDbContext(options, new AesGcmFieldEncryptionService("unit-test-key"));
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = nameof(ConfigurationAndEncryptionTests);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
