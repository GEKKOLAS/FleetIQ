using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Infrastructure.Outbox;
using FleetIQ.Compliance.Application.Abstractions;
using FleetIQ.Compliance.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetIQ.Compliance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddComplianceInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ComplianceDbContext>(o => o.UseNpgsql(config.GetConnectionString("Compliance")));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ComplianceDbContext>());
        services.AddScoped<IComplianceCheckRepository, ComplianceCheckRepository>();
        services.AddHostedService<OutboxProcessor<ComplianceDbContext>>();

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
