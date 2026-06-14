using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FleetIQ.BuildingBlocks.Web;

/// <summary>Native .NET 10 OpenAPI. Produces the OpenAPI document at /openapi/v1.json.
/// For a browser UI add Scalar (see README-fix.md) - kept out here to guarantee the build.</summary>
public static class OpenApiExtensions
{
    public static IServiceCollection AddFleetIqSwagger(this IServiceCollection services)
    {
        services.AddOpenApi();
        return services;
    }

    public static WebApplication MapFleetIqSwagger(this WebApplication app)
    {
        app.MapOpenApi();   // GET /openapi/v1.json
        return app;
    }
}
