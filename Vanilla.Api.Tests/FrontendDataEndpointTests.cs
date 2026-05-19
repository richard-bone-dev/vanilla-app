using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Vanilla.Api.Tests;

public sealed class FrontendDataEndpointTests
{
    [Fact]
    public async Task Lists_active_customers_with_current_balances()
    {
        await using var factory = new TestApiFactory();
        await factory.SeedAsync(dbContext =>
        {
            var alice = new Customer { Name = "Alice" };
            var bob = new Customer { Name = "Bob" };
            dbContext.Customers.AddRange(
                alice,
                bob,
                new Customer { Name = "Deleted", IsDeleted = true, DeletedUtc = DateTime.UtcNow, DeletedReason = "Settled" });
            dbContext.Orders.Add(new Order { Customer = alice, Amount = 30m, CreatedUtc = DateTime.UtcNow });
            dbContext.Payments.Add(new Payment { Customer = alice, Amount = 5m, CreatedUtc = DateTime.UtcNow });
            return Task.CompletedTask;
        });

        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<List<CustomerSummaryResponse>>("/api/customers");

        response.Should().HaveCount(2);
        response!.Select(customer => customer.Name).Should().Equal("Alice", "Bob");
        response.Single(customer => customer.Name == "Alice").CurrentBalance.Should().Be(25m);
    }

    [Fact]
    public async Task Lists_order_and_payment_entries_newest_first()
    {
        await using var factory = new TestApiFactory();
        await factory.SeedAsync(dbContext =>
        {
            var customer = new Customer { Name = "Alice" };
            dbContext.Customers.Add(customer);
            dbContext.Orders.Add(new Order
            {
                Customer = customer,
                Amount = 30m,
                Notes = "older order",
                CreatedUtc = new DateTime(2026, 05, 18, 10, 00, 00, DateTimeKind.Utc)
            });
            dbContext.Payments.Add(new Payment
            {
                Customer = customer,
                Amount = 5m,
                Notes = "newer payment",
                CreatedUtc = new DateTime(2026, 05, 19, 10, 00, 00, DateTimeKind.Utc)
            });
            return Task.CompletedTask;
        });

        var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<List<LedgerEntryListItem>>("/api/ledger/entries");

        response.Should().HaveCount(2);
        response![0].EntryType.Should().Be("Payment");
        response[0].CustomerName.Should().Be("Alice");
        response[1].EntryType.Should().Be("Order");
    }

    [Fact]
    public async Task Customer_creation_can_persist_opening_balance_as_initial_entry()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerRequest("Opening Customer", null, null, null, 24.50m));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var customer = await response.Content.ReadFromJsonAsync<CustomerSummaryResponse>();
        customer!.CurrentBalance.Should().Be(24.50m);

        var entries = await client.GetFromJsonAsync<List<LedgerEntryListItem>>("/api/ledger/entries");
        entries.Should().ContainSingle(entry =>
            entry.CustomerId == customer.Id
            && entry.EntryType == "Order"
            && entry.Notes == "Opening balance"
            && entry.Amount == 24.50m);
    }

    [Fact]
    public async Task Clear_ledger_data_soft_deletes_active_records()
    {
        await using var factory = new TestApiFactory();
        await factory.SeedAsync(dbContext =>
        {
            var customer = new Customer { Name = "Alice" };
            dbContext.Customers.Add(customer);
            dbContext.Orders.Add(new Order { Customer = customer, Amount = 30m, CreatedUtc = DateTime.UtcNow });
            dbContext.Payments.Add(new Payment { Customer = customer, Amount = 5m, CreatedUtc = DateTime.UtcNow });
            return Task.CompletedTask;
        });

        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/ledger");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ClearLedgerDataResponse>();
        payload!.TotalRowsSoftDeleted.Should().Be(3);

        var customers = await client.GetFromJsonAsync<List<CustomerSummaryResponse>>("/api/customers");
        var entries = await client.GetFromJsonAsync<List<LedgerEntryListItem>>("/api/ledger/entries");
        var dashboard = await client.GetFromJsonAsync<DashboardSummaryResponse>("/api/dashboard/summary");

        customers.Should().BeEmpty();
        entries.Should().BeEmpty();
        dashboard!.OutstandingBalance.Should().Be(0m);
    }
}
