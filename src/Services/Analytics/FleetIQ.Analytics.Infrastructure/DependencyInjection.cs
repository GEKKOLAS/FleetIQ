using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Infrastructure.Outbox;
using FleetIQ.Analytics.Application.Abstractions;
using FleetIQ.Analytics.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetIQ.Analytics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAnalyticsInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AnalyticsDbContext>(o => o.UseNpgsql(config.GetConnectionString("Analytics")));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AnalyticsDbContext>());
        services.AddScoped<IReportDefinitionRepository, ReportDefinitionRepository>();
        services.AddHostedService<OutboxProcessor<AnalyticsDbContext>>();

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
