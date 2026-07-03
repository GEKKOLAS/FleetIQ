using FleetIQ.Contracts;
using FleetIQ.Planning.Infrastructure.Persistence;
using FleetIQ.Planning.Infrastructure.Persistence.ReadModels;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FleetIQ.Planning.Infrastructure.Consumers;

/// <summary>Updates the local carrier read-model when a carrier becomes verified (or is reactivated).</summary>
public sealed class CarrierVerifiedConsumer(PlanningDbContext db, ILogger<CarrierVerifiedConsumer> logger)
    : IConsumer<CarrierVerifiedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<CarrierVerifiedIntegrationEvent> context)
    {
        var m = context.Message;
        var view = await db.Set<CarrierView>().FirstOrDefaultAsync(v => v.CarrierId == m.CarrierId, context.CancellationToken);
        if (view is null)
        {
            view = new CarrierView { CarrierId = m.CarrierId };
            db.Set<CarrierView>().Add(view);
        }
        view.LegalName = m.LegalName;
        view.IsVerified = true;
        view.UpdatedOnUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("[Planning] Carrier {CarrierId} ({Name}) is now assignable.", m.CarrierId, m.LegalName);
    }
}
