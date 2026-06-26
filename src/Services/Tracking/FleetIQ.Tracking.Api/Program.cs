using System.Reflection;
using FleetIQ.BuildingBlocks.Web;
using FleetIQ.Tracking.Api;
using FleetIQ.Tracking.Api.Endpoints;
using FleetIQ.Tracking.Api.Hubs;
using FleetIQ.Tracking.Application.Abstractions;
using FleetIQ.Tracking.Application.CreateShipment;
using FleetIQ.Tracking.Infrastructure;
using FleetIQ.Tracking.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

builder.Services.AddTrackingInfrastructure(builder.Configuration);

var applicationAssembly = typeof(CreateShipmentCommand).Assembly;
builder.Services.AddValidatorsFromAssembly(applicationAssembly);
RegisterHandlers(builder.Services, applicationAssembly);

builder.Services.AddSignalR();
builder.Services.AddScoped<IShipmentLocationBroadcaster, ShipmentLocationBroadcaster>();
builder.Services.AddHostedService<LocationSimulator>();   // dev: moves shipments on the map

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddFleetIqSwagger();
builder.Services.AddFleetIqJwtAuth(builder.Configuration);

builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddOtlpExporter())
    .WithMetrics(m => m.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddOtlpExporter());

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TrackingDbContext>();
    db.Database.Migrate();
    await SeedData.SeedAsync(db);
    app.MapFleetIqSwagger();
}

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapShipmentEndpoints();
app.MapHub<TrackingHub>("/hubs/tracking");

app.Run();

static void RegisterHandlers(IServiceCollection services, Assembly assembly)
{
    var ifaces = new[] { typeof(FleetIQ.BuildingBlocks.Application.ICommandHandler<,>), typeof(FleetIQ.BuildingBlocks.Application.IQueryHandler<,>) };
    foreach (var type in assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false }))
        foreach (var i in type.GetInterfaces().Where(i => i.IsGenericType && ifaces.Contains(i.GetGenericTypeDefinition())))
            services.AddScoped(i, type);
}
