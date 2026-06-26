namespace FleetIQ.Contracts;

/// <summary>Published by Billing when an invoice is issued. Consumed by other contexts
/// (e.g. Notifications). This is the SHARED contract - services never reference each other's domain.</summary>
public sealed record InvoiceIssuedIntegrationEvent(
    Guid InvoiceId, Guid CustomerId, decimal Total, string Currency, DateTime DueDateUtc);

/// <summary>Published by Planning when a load is confirmed. Consumed by Tracking, which creates
/// the Shipment that executes the load (planning -> tracking handoff).</summary>
public sealed record LoadConfirmedIntegrationEvent(
    Guid LoadId, Guid CarrierId, string OriginCity, string DestinationCity, decimal WeightKg, DateTime ConfirmedOnUtc);
