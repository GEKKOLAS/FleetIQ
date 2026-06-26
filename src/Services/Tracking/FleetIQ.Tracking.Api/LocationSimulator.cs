using FleetIQ.Tracking.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FleetIQ.Tracking.Api;

/// <summary>Dev-only: broadcasts simulated positions for active shipments along a Lisbon -> Porto route.
/// READ-ONLY against the DB (no writes) so it never competes with the endpoints for row locks.</summary>
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
                var broadcaster = scope.ServiceProvider.GetRequiredService<IShipmentLocationBroadcaster>();

                var active = await repo.GetActiveAsync(stoppingToken); // read-only
                var wp = Route[step % Route.Length];
                foreach (var s in active)
                {
                    var lat = wp.Lat + (Random.Shared.NextDouble() - 0.5) * 0.05;
                    var lng = wp.Lng + (Random.Shared.NextDouble() - 0.5) * 0.05;
                    await broadcaster.BroadcastAsync(s.Id, lat, lng, 70 + Random.Shared.Next(20), s.Status.ToString(), DateTime.UtcNow, stoppingToken);
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
