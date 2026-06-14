using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Infrastructure.Outbox;
using FleetIQ.Carriers.Application.Abstractions;
using FleetIQ.Carriers.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetIQ.Carriers.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCarriersInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<CarriersDbContext>(o => o.UseNpgsql(config.GetConnectionString("Carriers")));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CarriersDbContext>());
        services.AddScoped<ICarrierRepository, CarrierRepository>();
        services.AddHostedService<OutboxProcessor<CarriersDbContext>>();

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
