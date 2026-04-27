using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Vanilla.Api.Tests;

public sealed class QuickEntryTests
{
    [Fact]
    public async Task Creates_order_for_existing_customer()
    {
        await using var factory = new TestApiFactory();
        var customerId = await SeedCustomerAsync(factory, "Alice");
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/quick-entry", new QuickEntryRequest("Order", customerId, null, 25m, "order note", false));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<QuickEntryResponse>();
        payload!.CreatedItem!.EntryType.Should().Be("Order");
        payload.ResultingBalance.Should().Be(25m);
    }

    [Fact]
    public async Task Creates_payment_for_existing_customer_and_payment_is_never_linked_to_order()
    {
        await using var factory = new TestApiFactory();
        var customerId = await SeedCustomerAsync(factory, "Alice");
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest(customerId, 20m, "seed order"));
        var response = await client.PostAsJsonAsync("/api/quick-entry", new QuickEntryRequest("Payment", customerId, null, 5m, "payment note", false));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<QuickEntryResponse>();
        payload!.CreatedItem!.EntryType.Should().Be("Payment");
        typeof(Payment).GetProperty("OrderId").Should().BeNull();
    }

    [Fact]
    public async Task Payment_can_create_negative_balance()
    {
        await using var factory = new TestApiFactory();
        var customerId = await SeedCustomerAsync(factory, "Alice");
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/quick-entry", new QuickEntryRequest("Payment", customerId, null, 10m, null, false));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<QuickEntryResponse>())!.ResultingBalance.Should().Be(-10m);
    }

    [Fact]
    public async Task Unknown_customer_with_auto_create_customer_if_missing_false_returns_requires_customer_confirmation()
    {
        await using var factory = new TestApiFactory();
        var response = await factory.CreateClient().PostAsJsonAsync("/api/quick-entry", new QuickEntryRequest("Order", null, "Missing Customer", 12m, null, false));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<QuickEntryResponse>())!.RequiresCustomerConfirmation.Should().BeTrue();
    }

    [Fact]
    public async Task Unknown_customer_with_auto_create_customer_if_missing_true_creates_customer_and_entry()
    {
        await using var factory = new TestApiFactory();
        var response = await factory.CreateClient().PostAsJsonAsync("/api/quick-entry", new QuickEntryRequest("Order", null, "New Customer", 12m, null, true));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<QuickEntryResponse>();
        payload!.CreatedItem.Should().NotBeNull();
        payload.Customer!.Name.Should().Be("New Customer");
    }

    [Fact]
    public async Task Missing_customer_fails_validation()
    {
        await using var factory = new TestApiFactory();
        (await factory.CreateClient().PostAsJsonAsync("/api/quick-entry", new QuickEntryRequest("Order", null, null, 10m, null, false))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task Zero_or_negative_amount_fails_validation(decimal amount)
    {
        await using var factory = new TestApiFactory();
        var customerId = await SeedCustomerAsync(factory, "Alice");
        (await factory.CreateClient().PostAsJsonAsync("/api/quick-entry", new QuickEntryRequest("Payment", customerId, null, amount, null, false))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Payment_reducing_balance_to_zero_returns_requires_settlement_confirmation()
    {
        await using var factory = new TestApiFactory();
        var customerId = await SeedCustomerAsync(factory, "Alice");
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest(customerId, 20m, null));
        var response = await client.PostAsJsonAsync("/api/quick-entry", new QuickEntryRequest("Payment", customerId, null, 20m, null, false));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<QuickEntryResponse>();
        payload!.RequiresSettlementConfirmation.Should().BeTrue();
        payload.ResultingBalance.Should().Be(0m);
    }

    private static async Task<Guid> SeedCustomerAsync(TestApiFactory factory, string name)
    {
        var customerId = Guid.Empty;
        await factory.SeedAsync(dbContext =>
        {
            var customer = new Customer { Name = name };
            dbContext.Customers.Add(customer);
            customerId = customer.Id;
            return Task.CompletedTask;
        });
        return customerId;
    }
}