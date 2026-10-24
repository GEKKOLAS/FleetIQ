using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Infrastructure.Outbox;
using FleetIQ.Identity.Application.Abstractions;
using FleetIQ.Identity.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetIQ.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<IdentityDbContext>(o => o.UseNpgsql(config.GetConnectionString("Identity")));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddHostedService<OutboxProcessor<IdentityDbContext>>();

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
