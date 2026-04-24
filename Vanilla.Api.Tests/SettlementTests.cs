using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Vanilla.Api.Tests;

public sealed class SettlementTests
{
    [Fact]
    public async Task Settlement_fails_if_active_balance_is_not_zero()
    {
        await using var factory = new TestApiFactory();
        var customerId = await SeedCustomerWithEntriesAsync(factory, 20m, 5m);
        var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/customers/{customerId}/settle", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Settlement_succeeds_if_active_balance_is_zero_soft_deletes_everything_and_returns_row_counts()
    {
        await using var factory = new TestApiFactory();
        var customerId = await SeedCustomerWithEntriesAsync(factory, 20m, 20m);
        var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/customers/{customerId}/settle", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<SettlementResponse>();
        payload!.CustomersSoftDeleted.Should().Be(1);
        payload.OrdersSoftDeleted.Should().Be(1);
        payload.PaymentsSoftDeleted.Should().Be(1);
        payload.TotalRowsSoftDeleted.Should().Be(3);

        factory.Query(dbContext => dbContext.Customers.Count()).Should().Be(0);
        factory.Query(dbContext => dbContext.Orders.Count()).Should().Be(0);
        factory.Query(dbContext => dbContext.Payments.Count()).Should().Be(0);
    }

    [Fact]
    public async Task Settled_records_are_excluded_from_search_ledger_and_dashboard_summary()
    {
        await using var factory = new TestApiFactory();
        var customerId = await SeedCustomerWithEntriesAsync(factory, 20m, 20m);
        var client = factory.CreateClient();

        await client.PostAsync($"/api/customers/{customerId}/settle", null);

        var search = await client.GetFromJsonAsync<List<CustomerSearchItem>>("/api/customers/search?query=Ali");
        var ledger = await client.GetAsync($"/api/customers/{customerId}/ledger");
        var dashboard = await client.GetFromJsonAsync<DashboardSummaryResponse>("/api/dashboard/summary");

        search.Should().BeEmpty();
        ledger.StatusCode.Should().Be(HttpStatusCode.NotFound);
        dashboard!.ActiveCustomerCount.Should().Be(0);
        dashboard.ActiveOrderTotal.Should().Be(0m);
        dashboard.ActivePaymentTotal.Should().Be(0m);
        dashboard.OutstandingBalance.Should().Be(0m);
    }

    private static async Task<Guid> SeedCustomerWithEntriesAsync(TestApiFactory factory, decimal orderAmount, decimal paymentAmount)
    {
        var customerId = Guid.Empty;
        await factory.SeedAsync(dbContext =>
        {
            var customer = new Customer { Name = "Alice" };
            dbContext.Customers.Add(customer);
            dbContext.Orders.Add(new Order { Customer = customer, Amount = orderAmount, CreatedUtc = DateTime.UtcNow });
            dbContext.Payments.Add(new Payment { Customer = customer, Amount = paymentAmount, CreatedUtc = DateTime.UtcNow });
            customerId = customer.Id;
            return Task.CompletedTask;
        });

        return customerId;
    }
}
