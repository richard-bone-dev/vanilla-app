namespace Vanilla.Api;

public sealed record CreateCustomerRequest(
    string Name,
    string? Email,
    string? Phone,
    string? Notes);

public sealed record CreateOrderRequest(
    Guid CustomerId,
    decimal Amount,
    string? Notes);

public sealed record CreatePaymentRequest(
    Guid CustomerId,
    decimal Amount,
    string? Notes);

public sealed record QuickEntryRequest(
    string EntryType,
    Guid? CustomerId,
    string? CustomerName,
    decimal Amount,
    string? Note,
    bool AutoCreateCustomerIfMissing);

public sealed record CustomerSearchItem(Guid Id, string Name, decimal CurrentBalance);

public sealed record CustomerSummaryResponse(Guid Id, string Name, decimal CurrentBalance);

public sealed record LedgerEntryResponse(Guid Id, decimal Amount, string? Notes, DateTime CreatedUtc);

public sealed record CustomerLedgerResponse(
    CustomerSummaryResponse Customer,
    IReadOnlyList<LedgerEntryResponse> Orders,
    IReadOnlyList<LedgerEntryResponse> Payments);

public sealed record CreatedItemResponse(
    Guid Id,
    string EntryType,
    Guid CustomerId,
    decimal Amount,
    string? Notes,
    DateTime CreatedUtc);

public sealed record QuickEntryResponse(
    CreatedItemResponse? CreatedItem,
    CustomerSummaryResponse? Customer,
    decimal? ResultingBalance,
    bool RequiresCustomerConfirmation,
    bool RequiresSettlementConfirmation,
    string? Message);

public sealed record SettlementResponse(
    int CustomersSoftDeleted,
    int OrdersSoftDeleted,
    int PaymentsSoftDeleted,
    int TotalRowsSoftDeleted);

public sealed record DashboardSummaryResponse(
    int ActiveCustomerCount,
    decimal ActiveOrderTotal,
    decimal ActivePaymentTotal,
    decimal OutstandingBalance);
