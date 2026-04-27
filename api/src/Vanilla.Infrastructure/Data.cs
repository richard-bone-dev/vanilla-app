using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Vanilla.Application;
using Vanilla.Domain;
using Vanilla.Infrastructure.Security;

namespace Vanilla.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IFieldEncryptionService encryptionService) : DbContext(options), IAppDbContext
{
    private readonly IFieldEncryptionService _encryptionService = encryptionService;

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Payment> Payments => Set<Payment>();

    DatabaseFacade IAppDbContext.Database => Database;
    EntityEntry IAppDbContext.Add(object entity) => base.Add(entity);
    Task<int> IAppDbContext.SaveChangesAsync(CancellationToken cancellationToken) => base.SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var encryptedConverter = new ValueConverter<string?, string?>(value => _encryptionService.Encrypt(value), value => _encryptionService.Decrypt(value));

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(customer => customer.Id);
            entity.Property(customer => customer.Name).HasMaxLength(200).IsRequired();
            entity.Property(customer => customer.Email).HasConversion(encryptedConverter);
            entity.Property(customer => customer.Phone).HasConversion(encryptedConverter);
            entity.Property(customer => customer.Notes).HasConversion(encryptedConverter);
            entity.HasIndex(customer => new { customer.IsDeleted, customer.Name });
            entity.HasQueryFilter(customer => !customer.IsDeleted);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Amount).HasPrecision(18, 2);
            entity.Property(order => order.Notes).HasConversion(encryptedConverter);
            entity.HasIndex(order => new { order.CustomerId, order.IsDeleted, order.CreatedUtc });
            entity.HasOne(order => order.Customer).WithMany(customer => customer.Orders).HasForeignKey(order => order.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(order => !order.IsDeleted);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(payment => payment.Id);
            entity.Property(payment => payment.Amount).HasPrecision(18, 2);
            entity.Property(payment => payment.Notes).HasConversion(encryptedConverter);
            entity.HasIndex(payment => new { payment.CustomerId, payment.IsDeleted, payment.CreatedUtc });
            entity.HasOne(payment => payment.Customer).WithMany(customer => customer.Payments).HasForeignKey(payment => payment.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(payment => !payment.IsDeleted);
        });

        ConfigureSoftDelete(modelBuilder.Entity<Customer>());
        ConfigureSoftDelete(modelBuilder.Entity<Order>());
        ConfigureSoftDelete(modelBuilder.Entity<Payment>());
    }

    private static void ConfigureSoftDelete<TEntity>(EntityTypeBuilder<TEntity> entity) where TEntity : class, ISoftDeletable
    {
        entity.Property(item => item.IsDeleted).HasDefaultValue(false);
        entity.Property(item => item.DeletedReason).HasMaxLength(250);
    }
}

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (dbContext.Database.IsSqlServer())
        {
            await dbContext.Database.MigrateAsync();
            return;
        }

        await dbContext.Database.EnsureCreatedAsync();
    }
}