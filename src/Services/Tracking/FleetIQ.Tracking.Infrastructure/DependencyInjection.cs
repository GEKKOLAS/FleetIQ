using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Infrastructure.Outbox;
using FleetIQ.Tracking.Application.Abstractions;
using FleetIQ.Tracking.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetIQ.Tracking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTrackingInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<TrackingDbContext>(o => o.UseNpgsql(config.GetConnectionString("Tracking")));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TrackingDbContext>());
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddHostedService<OutboxProcessor<TrackingDbContext>>();

        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(config.GetConnectionString("RabbitMq") ?? "amqp://fleetiq:REDACTED@localhost:5672");
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
