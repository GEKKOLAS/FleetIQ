using FleetIQ.Contracts;
using FleetIQ.Planning.Infrastructure.Persistence;
using FleetIQ.Planning.Infrastructure.Persistence.ReadModels;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FleetIQ.Planning.Infrastructure.Consumers;

/// <summary>Marks a carrier not-assignable in the local read-model when it is suspended.</summary>
public sealed class CarrierSuspendedConsumer(PlanningDbContext db, ILogger<CarrierSuspendedConsumer> logger)
    : IConsumer<CarrierSuspendedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<CarrierSuspendedIntegrationEvent> context)
    {
        var m = context.Message;
        var view = await db.Set<CarrierView>().FirstOrDefaultAsync(v => v.CarrierId == m.CarrierId, context.CancellationToken);
        if (view is not null)
        {
            view.IsVerified = false;
            view.UpdatedOnUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(context.CancellationToken);
            logger.LogInformation("[Planning] Carrier {CarrierId} suspended -> not assignable.", m.CarrierId);
        }
    }
}
