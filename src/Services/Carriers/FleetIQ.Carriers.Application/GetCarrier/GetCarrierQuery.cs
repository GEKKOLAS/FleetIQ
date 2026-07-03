using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Carriers.Application.Abstractions;

namespace FleetIQ.Carriers.Application.GetCarrier;

public sealed record GetCarrierQuery(Guid Id) : IQuery<CarrierDetailResponse?>;

public sealed record VehicleResponse(Guid Id, string Plate, string Type, decimal CapacityKg, string Status);
public sealed record DriverResponse(Guid Id, string FullName, string LicenceNumber, string Status);

public sealed record CarrierDetailResponse(
    Guid Id, string LegalName, string RegistrationNumber, string Country, string Status,
    decimal TotalCapacityKg, int FleetSize, int DriverCount,
    DateTime CreatedOnUtc, DateTime? VerifiedOnUtc, DateTime? SuspendedOnUtc,
    IReadOnlyList<VehicleResponse> Vehicles, IReadOnlyList<DriverResponse> Drivers);

public sealed class GetCarrierQueryHandler(ICarrierRepository repository)
    : IQueryHandler<GetCarrierQuery, CarrierDetailResponse?>
{
    public async Task<CarrierDetailResponse?> Handle(GetCarrierQuery query, CancellationToken ct)
    {
        var c = await repository.GetByIdAsync(query.Id, ct);
        if (c is null) return null;

        return new CarrierDetailResponse(
            c.Id, c.LegalName, c.RegistrationNumber, c.Country, c.Status.ToString(),
            c.TotalCapacityKg, c.FleetSize, c.DriverCount, c.CreatedOnUtc, c.VerifiedOnUtc, c.SuspendedOnUtc,
            c.Vehicles.Select(v => new VehicleResponse(v.Id, v.Plate, v.Type.ToString(), v.CapacityKg, v.Status.ToString())).ToList(),
            c.Drivers.Select(d => new DriverResponse(d.Id, d.FullName, d.LicenceNumber, d.Status.ToString())).ToList());
    }
}
