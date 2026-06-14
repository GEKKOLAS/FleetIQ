using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetIQ.BuildingBlocks.Web;

/// <summary>Shared JWT bearer setup so every service validates tokens from the IdentityServer the same way.</summary>
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
            });

        services.AddAuthorization();
        return services;
    }
}
