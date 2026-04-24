using Vanilla.Api.Services;

namespace Vanilla.Api;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapLedgerApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api");

        group.MapGet("/customers/search", async (
            string? query,
            LedgerApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var results = await service.SearchCustomersAsync(query, cancellationToken);
            return Results.Ok(results);
        });

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
        });

        group.MapGet("/customers/{id:guid}/ledger", async (
            Guid id,
            LedgerApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var ledger = await service.GetLedgerAsync(id, cancellationToken);
            return ledger is null ? Results.NotFound() : Results.Ok(ledger);
        });

        group.MapPost("/orders", async (
            CreateOrderRequest request,
            LedgerApplicationService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await service.CreateOrderAsync(request, cancellationToken);
            return ToEntryResult(result, httpContext.Request.Path);
        });

        group.MapPost("/payments", async (
            CreatePaymentRequest request,
            LedgerApplicationService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await service.CreatePaymentAsync(request, cancellationToken);
            return ToEntryResult(result, httpContext.Request.Path);
        });

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
        });

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
                return Results.BadRequest(new
                {
                    message = "Customer can only be settled when the active balance is zero.",
                    activeBalance = result.ActiveBalance
                });
            }

            return Results.Ok(result.Response);
        });

        group.MapGet("/dashboard/summary", async (
            LedgerApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var response = await service.GetDashboardSummaryAsync(cancellationToken);
            return Results.Ok(response);
        });

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

        return Results.Created(path, new
        {
            createdItem = result.CreatedItem,
            customer = result.Customer,
            resultingBalance = result.ResultingBalance,
            requiresSettlementConfirmation = result.RequiresSettlementConfirmation
        });
    }
}
