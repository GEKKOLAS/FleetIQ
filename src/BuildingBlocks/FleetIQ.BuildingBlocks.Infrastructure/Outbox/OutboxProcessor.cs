using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FleetIQ.BuildingBlocks.Infrastructure.Outbox;

/// <summary>Polls the outbox and publishes pending messages to RabbitMQ via MassTransit.
/// Generic over the service's DbContext so every service can reuse it.</summary>
public sealed class OutboxProcessor<TContext>(
    IServiceScopeFactory scopeFactory,
    IBus bus,
    ILogger<OutboxProcessor<TContext>> logger) : BackgroundService
    where TContext : DbContext
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<TContext>();

                var pending = await db.Set<OutboxMessage>()
                    .Where(m => m.ProcessedOnUtc == null)
                    .OrderBy(m => m.OccurredOnUtc)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var message in pending)
                {
                    var type = Type.GetType(message.Type);
                    if (type is null) { message.Error = "Unknown type"; message.ProcessedOnUtc = DateTime.UtcNow; continue; }

                    var payload = JsonSerializer.Deserialize(message.Content, type)!;
                    await bus.Publish(payload, type, stoppingToken);
                    message.ProcessedOnUtc = DateTime.UtcNow;
                }

                if (pending.Count > 0) await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox processing failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
