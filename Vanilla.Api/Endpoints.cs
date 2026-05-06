using Vanilla.Api.Services;

namespace Vanilla.Api;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapLedgerApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", (IConfiguration configuration, IHostEnvironment environment) =>
            Results.Ok(new ApiRootResponse(
                "Vanilla API",
                "running",
                "/health",
                "/api",
                AppConfiguration.IsApiDocsEnabled(configuration, environment) ? "/openapi/v1.json" : null,
                AppConfiguration.IsApiDocsEnabled(configuration, environment) ? "/docs" : null)))
            .WithName("GetApiRoot")
            .WithTags("Infrastructure")
            .WithSummary("Returns the API status and documentation routes.")
            .Produces<ApiRootResponse>(StatusCodes.Status200OK);

        var group = endpoints.MapGroup("/api");

        group.MapGet("/customers/search", async (
            string? query,
            LedgerApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var results = await service.SearchCustomersAsync(query, cancellationToken);
            return Results.Ok(results);
        })
            .WithName("SearchCustomers")
            .WithTags("Customers")
            .WithSummary("Searches customers by name.")
            .Produces<IReadOnlyList<CustomerSearchItem>>(StatusCodes.Status200OK);

        group.MapPost("/customers", async (
            CreateCustomerRequest request,
            LedgerApplicationService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = ["Name is required."]
                });
            }

            var customer = await service.CreateCustomerAsync(request, cancellationToken);
            return Results.Created($"{httpContext.Request.Path}/{customer.Id}", customer);
        })
            .WithName("CreateCustomer")
            .WithTags("Customers")
            .WithSummary("Creates a new customer.")
            .Accepts<CreateCustomerRequest>("application/json")
            .Produces<CustomerSummaryResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/customers/{id:guid}/ledger", async (
            Guid id,
            LedgerApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var ledger = await service.GetLedgerAsync(id, cancellationToken);
            return ledger is null ? Results.NotFound() : Results.Ok(ledger);
        })
            .WithName("GetCustomerLedger")
            .WithTags("Customers")
            .WithSummary("Gets a customer's ledger history.")
            .Produces<CustomerLedgerResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/orders", async (
            CreateOrderRequest request,
            LedgerApplicationService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await service.CreateOrderAsync(request, cancellationToken);
            return ToEntryResult(result, httpContext.Request.Path);
        })
            .WithName("CreateOrder")
            .WithTags("Orders")
            .WithSummary("Creates a new order entry.")
            .Accepts<CreateOrderRequest>("application/json")
            .Produces<CreatedEntryEnvelope>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/payments", async (
            CreatePaymentRequest request,
            LedgerApplicationService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await service.CreatePaymentAsync(request, cancellationToken);
            return ToEntryResult(result, httpContext.Request.Path);
        })
            .WithName("CreatePayment")
            .WithTags("Payments")
            .WithSummary("Creates a new payment entry.")
            .Accepts<CreatePaymentRequest>("application/json")
            .Produces<CreatedEntryEnvelope>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/quick-entry", async (
            QuickEntryRequest request,
            LedgerApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.CreateQuickEntryAsync(request, cancellationToken);
            if (result.ValidationErrors is not null)
            {
                return Results.ValidationProblem(result.ValidationErrors);
            }

            if (result.IsNotFound)
            {
                return Results.NotFound(result.Response);
            }

            return Results.Ok(result.Response);
        })
            .WithName("CreateQuickEntry")
            .WithTags("Entries")
            .WithSummary("Creates an order or payment with optional customer creation.")
            .Accepts<QuickEntryRequest>("application/json")
            .Produces<QuickEntryResponse>(StatusCodes.Status200OK)
            .Produces<QuickEntryResponse>(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/customers/{id:guid}/settle", async (
            Guid id,
            LedgerApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.SettleCustomerAsync(id, cancellationToken);
            if (result.IsNotFound)
            {
                return Results.NotFound();
            }

            if (result.ActiveBalance is not null)
            {
                return Results.BadRequest(new SettlementBlockedResponse(
                    "Customer can only be settled when the active balance is zero.",
                    result.ActiveBalance.Value));
            }

            return Results.Ok(result.Response);
        })
            .WithName("SettleCustomer")
            .WithTags("Customers")
            .WithSummary("Soft-deletes a fully settled customer and related entries.")
            .Produces<SettlementResponse>(StatusCodes.Status200OK)
            .Produces<SettlementBlockedResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/dashboard/summary", async (
            LedgerApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var response = await service.GetDashboardSummaryAsync(cancellationToken);
            return Results.Ok(response);
        })
            .WithName("GetDashboardSummary")
            .WithTags("Dashboard")
            .WithSummary("Gets the dashboard summary totals.")
            .Produces<DashboardSummaryResponse>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static IResult ToEntryResult(EntryMutationResult result, PathString path)
    {
        if (result.ValidationErrors is not null)
        {
            return Results.ValidationProblem(result.ValidationErrors);
        }

        if (result.IsNotFound)
        {
            return Results.NotFound();
        }

        return Results.Created(path, new CreatedEntryEnvelope(
            result.CreatedItem!,
            result.Customer!,
            result.ResultingBalance!.Value,
            result.RequiresSettlementConfirmation));
    }
}
