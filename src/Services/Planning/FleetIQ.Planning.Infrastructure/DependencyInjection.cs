using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Infrastructure.Messaging;
using FleetIQ.BuildingBlocks.Infrastructure.Outbox;
using FleetIQ.Planning.Application.Abstractions;
using FleetIQ.Planning.Infrastructure.Consumers;
using FleetIQ.Planning.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetIQ.Planning.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPlanningInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<PlanningDbContext>(o => o.UseNpgsql(config.GetConnectionString("Planning")));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PlanningDbContext>());
        services.AddScoped<ILoadRepository, LoadRepository>();
        services.AddScoped<IRouteRepository, RouteRepository>();
        services.AddScoped<ICarrierDirectory, CarrierDirectory>();
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
        services.AddHostedService<OutboxProcessor<PlanningDbContext>>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<CarrierVerifiedConsumer>();   // keep local carrier read-model in sync
            x.AddConsumer<CarrierSuspendedConsumer>();
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(config.GetConnectionString("RabbitMq") ?? "amqp://fleetiq:REDACTED@localhost:5672");
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
