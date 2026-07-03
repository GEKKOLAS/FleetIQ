using FleetIQ.Planning.Application.Abstractions;
using FleetIQ.Planning.Infrastructure.Persistence.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Planning.Infrastructure.Persistence;

public sealed class CarrierDirectory(PlanningDbContext db) : ICarrierDirectory
{
    public Task<bool> IsVerifiedAsync(Guid carrierId, CancellationToken ct) =>
        db.Set<CarrierView>().AnyAsync(v => v.CarrierId == carrierId && v.IsVerified, ct);
}
