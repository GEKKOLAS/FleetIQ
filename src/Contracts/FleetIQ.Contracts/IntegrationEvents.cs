namespace FleetIQ.Contracts;

/// <summary>Published by Billing when an invoice is issued. Consumed by other contexts
/// (e.g. Notifications). This is the SHARED contract - services never reference each other's domain.</summary>
public sealed record InvoiceIssuedIntegrationEvent(
    Guid InvoiceId, Guid CustomerId, decimal Total, string Currency, DateTime DueDateUtc);
