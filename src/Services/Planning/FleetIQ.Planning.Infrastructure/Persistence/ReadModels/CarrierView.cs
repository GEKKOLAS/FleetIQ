namespace FleetIQ.Planning.Infrastructure.Persistence.ReadModels;

/// <summary>Local projection of carriers, kept up to date by consuming Carrier integration events.
/// Planning uses it to validate carrier assignments without calling the Carriers service.</summary>
public sealed class CarrierView
{
    public Guid CarrierId { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public DateTime UpdatedOnUtc { get; set; }
}
