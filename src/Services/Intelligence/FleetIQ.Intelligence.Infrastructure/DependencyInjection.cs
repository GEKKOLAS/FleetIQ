using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Infrastructure.Outbox;
using FleetIQ.Intelligence.Application.Abstractions;
using FleetIQ.Intelligence.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetIQ.Intelligence.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIntelligenceInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<IntelligenceDbContext>(o => o.UseNpgsql(config.GetConnectionString("Intelligence")));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IntelligenceDbContext>());
        services.AddScoped<IPredictionRequestRepository, PredictionRequestRepository>();
        services.AddHostedService<OutboxProcessor<IntelligenceDbContext>>();

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
