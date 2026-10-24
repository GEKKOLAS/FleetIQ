using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Infrastructure.Outbox;
using FleetIQ.Integration.Application.Abstractions;
using FleetIQ.Integration.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetIQ.Integration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIntegrationInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<IntegrationDbContext>(o => o.UseNpgsql(config.GetConnectionString("Integration")));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IntegrationDbContext>());
        services.AddScoped<IIntegrationConnectorRepository, IntegrationConnectorRepository>();
        services.AddHostedService<OutboxProcessor<IntegrationDbContext>>();

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
