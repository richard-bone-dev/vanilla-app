using System.Net.Http.Json;
using FluentAssertions;

namespace Vanilla.Api.Tests;

public sealed class CustomerSearchTests
{
    [Fact]
    public async Task Query_under_three_chars_returns_empty_result()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        (await client.GetFromJsonAsync<List<CustomerSearchItem>>("/api/customers/search?query=ab")).Should().BeEmpty();
    }

    [Fact]
    public async Task Partial_customer_name_match_returns_active_customers_and_excludes_soft_deleted_customers()
    {
        await using var factory = new TestApiFactory();
        await factory.SeedAsync(dbContext =>
        {
            dbContext.Customers.AddRange(new Customer { Name = "Alice Johnson" }, new Customer { Name = "Alicia Keys" }, new Customer { Name = "Alice Deleted", IsDeleted = true, DeletedUtc = DateTime.UtcNow, DeletedReason = "Settled" });
            return Task.CompletedTask;
        });

        var client = factory.CreateClient();
        var response = await client.GetFromJsonAsync<List<CustomerSearchItem>>("/api/customers/search?query=Ali");
        response.Should().HaveCount(2);
        response!.Select(item => item.Name).Should().BeEquivalentTo(["Alice Johnson", "Alicia Keys"]);
    }
}