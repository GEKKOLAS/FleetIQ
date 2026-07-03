namespace FleetIQ.Contracts;

/// <summary>Published by Billing when an invoice is issued. Consumed by other contexts
/// (e.g. Notifications). This is the SHARED contract - services never reference each other's domain.</summary>
public sealed record InvoiceIssuedIntegrationEvent(
    Guid InvoiceId, Guid CustomerId, decimal Total, string Currency, DateTime DueDateUtc);

/// <summary>Published by Planning when a load is confirmed. Consumed by Tracking, which creates
/// the Shipment that executes the load (planning -> tracking handoff).</summary>
public sealed record LoadConfirmedIntegrationEvent(
    Guid LoadId, Guid CarrierId, string OriginCity, string DestinationCity, decimal WeightKg, DateTime ConfirmedOnUtc);

/// <summary>Published by Carriers when a carrier is verified (or reactivated). Consumed by Planning
/// to keep a local read-model used to validate carrier assignments.</summary>
public sealed record CarrierVerifiedIntegrationEvent(Guid CarrierId, string LegalName);

/// <summary>Published by Carriers when a carrier is suspended. Planning marks it not-assignable.</summary>
public sealed record CarrierSuspendedIntegrationEvent(Guid CarrierId);
