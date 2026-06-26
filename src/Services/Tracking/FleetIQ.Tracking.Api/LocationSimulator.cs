using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Tracking.Api.Hubs;
using FleetIQ.Tracking.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FleetIQ.Tracking.Api;

/// <summary>Dev-only: walks active shipments along a Lisbon -> Porto route every 2s.
/// Each tick uses its own DI scope, loads each active shipment tracked, records the new
/// location (advancing Dispatched -> InTransit and persisting the position) and broadcasts
/// it over SignalR. RecordLocation only updates scalar columns (no child rows), so it never
/// touches the child-collection insert path.</summary>
public sealed class LocationSimulator(IServiceScopeFactory scopeFactory, ILogger<LocationSimulator> logger) : BackgroundService
{
    private static readonly (double Lat, double Lng)[] Route =
    {
        (38.7223, -9.1393), (39.0, -9.05), (39.4, -8.9), (39.8, -8.7),
        (40.2, -8.6), (40.6, -8.65), (41.0, -8.6), (41.1496, -8.6109)
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var step = 1;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider.GetRequiredService<IShipmentRepository>();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var broadcaster = scope.ServiceProvider.GetRequiredService<IShipmentLocationBroadcaster>();

                var activeIds = (await repo.GetActiveAsync(stoppingToken)).Select(s => s.Id).ToList();
                var wp = Route[step % Route.Length];

                foreach (var id in activeIds)
                {
                    var shipment = await repo.GetByIdAsync(id, stoppingToken);
                    if (shipment is null) continue;

                    var lat = wp.Lat + (Random.Shared.NextDouble() - 0.5) * 0.05;
                    var lng = wp.Lng + (Random.Shared.NextDouble() - 0.5) * 0.05;
                    var speed = 70 + Random.Shared.Next(20);

                    var result = shipment.RecordLocation(lat, lng, speed);
                    if (result.IsFailure) continue;

                    await uow.SaveChangesAsync(stoppingToken);
                    await broadcaster.BroadcastAsync(shipment.Id, lat, lng, speed, shipment.Status.ToString(), DateTime.UtcNow, stoppingToken);
                }

                step++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Location simulator tick failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
