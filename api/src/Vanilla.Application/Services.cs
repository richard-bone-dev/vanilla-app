using Microsoft.EntityFrameworkCore;
using Vanilla.Domain;

namespace Vanilla.Application;

public sealed class LedgerApplicationService(IAppDbContext dbContext, IClock clock)
{
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly IClock _clock = clock;

    public async Task<IReadOnlyList<CustomerSearchItem>> SearchCustomersAsync(string? query, CancellationToken cancellationToken)
    {
        var search = query?.Trim() ?? string.Empty;
        if (search.Length < 3)
        {
            return [];
        }

        return await _dbContext.Customers
            .AsNoTracking()
            .Where(customer => EF.Functions.Like(customer.Name, $"%{search}%"))
            .OrderBy(customer => customer.Name)
            .Select(customer => new CustomerSearchItem(
                customer.Id,
                customer.Name,
                (customer.Orders.Select(order => (decimal?)order.Amount).Sum() ?? 0m)
                - (customer.Payments.Select(payment => (decimal?)payment.Amount).Sum() ?? 0m)))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerSummaryResponse> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = new Customer
        {
            Name = request.Name.Trim(),
            Email = request.Email,
            Phone = request.Phone,
            Notes = request.Notes
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new CustomerSummaryResponse(customer.Id, customer.Name, 0m);
    }

    public async Task<CustomerLedgerResponse?> GetLedgerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers
            .AsNoTracking()
            .Where(item => item.Id == customerId)
            .Select(item => new CustomerSummaryResponse(
                item.Id,
                item.Name,
                (item.Orders.Select(order => (decimal?)order.Amount).Sum() ?? 0m)
                - (item.Payments.Select(payment => (decimal?)payment.Amount).Sum() ?? 0m)))
            .SingleOrDefaultAsync(cancellationToken);

        if (customer is null)
        {
            return null;
        }

        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.CustomerId == customerId)
            .OrderBy(order => order.CreatedUtc)
            .Select(order => new LedgerEntryResponse(order.Id, order.Amount, order.Notes, order.CreatedUtc))
            .ToListAsync(cancellationToken);

        var payments = await _dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.CustomerId == customerId)
            .OrderBy(payment => payment.CreatedUtc)
            .Select(payment => new LedgerEntryResponse(payment.Id, payment.Amount, payment.Notes, payment.CreatedUtc))
            .ToListAsync(cancellationToken);

        return new CustomerLedgerResponse(customer, orders, payments);
    }

    public Task<EntryMutationResult> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken) =>
        CreateEntryAsync(request.CustomerId, request.Amount, request.Notes, "Order", cancellationToken);

    public Task<EntryMutationResult> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken) =>
        CreateEntryAsync(request.CustomerId, request.Amount, request.Notes, "Payment", cancellationToken);

    public async Task<QuickEntryResult> CreateQuickEntryAsync(QuickEntryRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return QuickEntryResult.Validation("amount", "Amount must be greater than zero.");
        }

        Customer? customer;
        if (request.CustomerId is { } customerId)
        {
            customer = await _dbContext.Customers.SingleOrDefaultAsync(item => item.Id == customerId, cancellationToken);
            if (customer is null)
            {
                return QuickEntryResult.NotFound("Customer not found.");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.CustomerName))
            {
                return QuickEntryResult.Validation("customer", "A customerId or customerName is required.");
            }

            var customerName = request.CustomerName.Trim();
            var normalizedName = customerName.ToLowerInvariant();

            customer = await _dbContext.Customers
                .SingleOrDefaultAsync(item => item.Name.ToLower() == normalizedName, cancellationToken);

            if (customer is null && !request.AutoCreateCustomerIfMissing)
            {
                return QuickEntryResult.RequiresCustomerConfirmation(customerName);
            }

            if (customer is null)
            {
                customer = new Customer { Name = customerName };
                _dbContext.Customers.Add(customer);
            }
        }

        var entryType = NormalizeEntryType(request.EntryType);
        if (entryType is null)
        {
            return QuickEntryResult.Validation("entryType", "entryType must be Order or Payment.");
        }

        object entry = entryType == "Order"
            ? new Order
            {
                Customer = customer!,
                Amount = request.Amount,
                Notes = request.Note,
                CreatedUtc = _clock.UtcNow
            }
            : new Payment
            {
                Customer = customer!,
                Amount = request.Amount,
                Notes = request.Note,
                CreatedUtc = _clock.UtcNow
            };

        _dbContext.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var resultingBalance = await GetCustomerBalanceAsync(customer!.Id, cancellationToken);
        var createdItem = entryType == "Order"
            ? BuildCreatedItem((Order)entry)
            : BuildCreatedItem((Payment)entry);

        return QuickEntryResult.Success(new QuickEntryResponse(
            createdItem,
            new CustomerSummaryResponse(customer.Id, customer.Name, resultingBalance),
            resultingBalance,
            false,
            entryType == "Payment" && resultingBalance == 0m,
            null));
    }

    public async Task<SettlementResult> SettleCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Customers.AnyAsync(customer => customer.Id == customerId, cancellationToken);
        if (!exists)
        {
            return SettlementResult.NotFound();
        }

        var activeBalance = await GetCustomerBalanceAsync(customerId, cancellationToken);
        if (activeBalance != 0m)
        {
            return SettlementResult.BalanceNotZero(activeBalance);
        }

        var deletedUtc = _clock.UtcNow;
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var customersSoftDeleted = await _dbContext.Customers
            .IgnoreQueryFilters()
            .Where(customer => customer.Id == customerId && !customer.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(customer => customer.IsDeleted, true)
                .SetProperty(customer => customer.DeletedUtc, deletedUtc)
                .SetProperty(customer => customer.DeletedReason, "Settled"), cancellationToken);

        var ordersSoftDeleted = await _dbContext.Orders
            .IgnoreQueryFilters()
            .Where(order => order.CustomerId == customerId && !order.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(order => order.IsDeleted, true)
                .SetProperty(order => order.DeletedUtc, deletedUtc)
                .SetProperty(order => order.DeletedReason, "Settled"), cancellationToken);

        var paymentsSoftDeleted = await _dbContext.Payments
            .IgnoreQueryFilters()
            .Where(payment => payment.CustomerId == customerId && !payment.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(payment => payment.IsDeleted, true)
                .SetProperty(payment => payment.DeletedUtc, deletedUtc)
                .SetProperty(payment => payment.DeletedReason, "Settled"), cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return SettlementResult.Success(new SettlementResponse(
            customersSoftDeleted,
            ordersSoftDeleted,
            paymentsSoftDeleted,
            customersSoftDeleted + ordersSoftDeleted + paymentsSoftDeleted));
    }

    public async Task<DashboardSummaryResponse> GetDashboardSummaryAsync(CancellationToken cancellationToken)
    {
        var activeCustomerCount = await _dbContext.Customers.CountAsync(cancellationToken);
        var activeOrderTotal = await _dbContext.Orders.SumAsync(order => (decimal?)order.Amount, cancellationToken) ?? 0m;
        var activePaymentTotal = await _dbContext.Payments.SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0m;

        return new DashboardSummaryResponse(
            activeCustomerCount,
            activeOrderTotal,
            activePaymentTotal,
            activeOrderTotal - activePaymentTotal);
    }

    private async Task<EntryMutationResult> CreateEntryAsync(Guid customerId, decimal amount, string? notes, string entryType, CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            return EntryMutationResult.Validation("amount", "Amount must be greater than zero.");
        }

        var customer = await _dbContext.Customers.SingleOrDefaultAsync(item => item.Id == customerId, cancellationToken);
        if (customer is null)
        {
            return EntryMutationResult.NotFound();
        }

        CreatedItemResponse createdItem;
        if (entryType == "Order")
        {
            var order = new Order
            {
                CustomerId = customerId,
                Amount = amount,
                Notes = notes,
                CreatedUtc = _clock.UtcNow
            };

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync(cancellationToken);
            createdItem = BuildCreatedItem(order);
        }
        else
        {
            var payment = new Payment
            {
                CustomerId = customerId,
                Amount = amount,
                Notes = notes,
                CreatedUtc = _clock.UtcNow
            };

            _dbContext.Payments.Add(payment);
            await _dbContext.SaveChangesAsync(cancellationToken);
            createdItem = BuildCreatedItem(payment);
        }

        var resultingBalance = await GetCustomerBalanceAsync(customerId, cancellationToken);

        return EntryMutationResult.Success(createdItem, new CustomerSummaryResponse(customer.Id, customer.Name, resultingBalance), resultingBalance, entryType == "Payment" && resultingBalance == 0m);
    }

    private async Task<decimal> GetCustomerBalanceAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var orderTotal = await _dbContext.Orders.Where(order => order.CustomerId == customerId).SumAsync(order => (decimal?)order.Amount, cancellationToken) ?? 0m;
        var paymentTotal = await _dbContext.Payments.Where(payment => payment.CustomerId == customerId).SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0m;
        return orderTotal - paymentTotal;
    }

    private static string? NormalizeEntryType(string? entryType)
    {
        if (string.Equals(entryType, "Order", StringComparison.OrdinalIgnoreCase))
        {
            return "Order";
        }

        if (string.Equals(entryType, "Payment", StringComparison.OrdinalIgnoreCase))
        {
            return "Payment";
        }

        return null;
    }

    private static CreatedItemResponse BuildCreatedItem(Order order) => new(order.Id, "Order", order.CustomerId, order.Amount, order.Notes, order.CreatedUtc);
    private static CreatedItemResponse BuildCreatedItem(Payment payment) => new(payment.Id, "Payment", payment.CustomerId, payment.Amount, payment.Notes, payment.CreatedUtc);
}

public sealed record EntryMutationResult(CreatedItemResponse? CreatedItem, CustomerSummaryResponse? Customer, decimal? ResultingBalance, bool RequiresSettlementConfirmation, bool IsNotFound, IReadOnlyDictionary<string, string[]>? ValidationErrors)
{
    public static EntryMutationResult Success(CreatedItemResponse createdItem, CustomerSummaryResponse customer, decimal resultingBalance, bool requiresSettlementConfirmation) =>
        new(createdItem, customer, resultingBalance, requiresSettlementConfirmation, false, null);

    public static EntryMutationResult NotFound() => new(null, null, null, false, true, null);

    public static EntryMutationResult Validation(string key, string message) =>
        new(null, null, null, false, false, new Dictionary<string, string[]> { [key] = [message] });
}

public sealed record QuickEntryResult(QuickEntryResponse? Response, bool IsNotFound, IReadOnlyDictionary<string, string[]>? ValidationErrors)
{
    public static QuickEntryResult Success(QuickEntryResponse response) => new(response, false, null);
    public static QuickEntryResult RequiresCustomerConfirmation(string customerName) => new(new QuickEntryResponse(null, null, null, true, false, $"Customer '{customerName}' was not found."), false, null);
    public static QuickEntryResult NotFound(string message) => new(new QuickEntryResponse(null, null, null, false, false, message), true, null);
    public static QuickEntryResult Validation(string key, string message) => new(null, false, new Dictionary<string, string[]> { [key] = [message] });
}

public sealed record SettlementResult(SettlementResponse? Response, bool IsNotFound, decimal? ActiveBalance)
{
    public static SettlementResult Success(SettlementResponse response) => new(response, false, null);
    public static SettlementResult NotFound() => new(null, true, null);
    public static SettlementResult BalanceNotZero(decimal balance) => new(null, false, balance);
}