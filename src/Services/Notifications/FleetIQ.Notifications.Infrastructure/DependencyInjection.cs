using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Infrastructure.Outbox;
using FleetIQ.Notifications.Application.Abstractions;
using FleetIQ.Notifications.Infrastructure.Consumers;
using FleetIQ.Notifications.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetIQ.Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<NotificationsDbContext>(o => o.UseNpgsql(config.GetConnectionString("Notifications")));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<NotificationsDbContext>());
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddHostedService<OutboxProcessor<NotificationsDbContext>>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<InvoiceIssuedConsumer>();   // <-- react to Billing's integration event
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
