using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Infrastructure.Outbox;
using FleetIQ.Orders.Application.Abstractions;
using FleetIQ.Orders.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetIQ.Orders.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrdersInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<OrdersDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("Orders")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OrdersDbContext>());
        services.AddScoped<IOrderRepository, OrderRepository>();

        services.AddHostedService<OutboxProcessor<OrdersDbContext>>();

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
