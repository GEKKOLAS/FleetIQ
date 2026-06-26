using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetIQ.BuildingBlocks.Web;

/// <summary>Shared JWT bearer setup so every service (and the gateway) validates tokens the same way.</summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddFleetIqJwtAuth(this IServiceCollection services, IConfiguration config)
    {
        var authority = config["Authentication:Authority"] ?? "https://localhost:5001";
        var audience = config["Authentication:Audience"] ?? "fleetiq.api";
        var requireHttps = !bool.TryParse(config["Authentication:RequireHttpsMetadata"], out var r) || r;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = requireHttps;

                // SignalR/WebSocket connections send the token via the query string, not the header.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }
}
