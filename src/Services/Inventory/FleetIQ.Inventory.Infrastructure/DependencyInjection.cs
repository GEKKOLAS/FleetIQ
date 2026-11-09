using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Infrastructure.Outbox;
using FleetIQ.Inventory.Application.Abstractions;
using FleetIQ.Inventory.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetIQ.Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<InventoryDbContext>(o => o.UseNpgsql(config.GetConnectionString("Inventory")));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<InventoryDbContext>());
        services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
        services.AddHostedService<OutboxProcessor<InventoryDbContext>>();

        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(config.GetConnectionString("RabbitMq")
                    ?? throw new InvalidOperationException("Missing ConnectionStrings:RabbitMq (run scripts/setup-secrets.ps1)"));
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
