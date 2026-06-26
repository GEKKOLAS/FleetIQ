using System.Reflection;
using FleetIQ.Orders.Api.Endpoints;
using FleetIQ.Orders.Application.CreateOrder;
using FleetIQ.Orders.Infrastructure;
using FleetIQ.Orders.Infrastructure.Persistence;
using FleetIQ.BuildingBlocks.Web;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

builder.Services.AddOrdersInfrastructure(builder.Configuration);

var applicationAssembly = typeof(CreateOrderCommand).Assembly;
builder.Services.AddValidatorsFromAssembly(applicationAssembly);
RegisterHandlers(builder.Services, applicationAssembly);

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
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    db.Database.Migrate();
    await SeedData.SeedAsync(db);
    app.MapFleetIqSwagger();
}

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapOrderEndpoints();

app.Run();

static void RegisterHandlers(IServiceCollection services, Assembly assembly)
{
    var openHandlerInterfaces = new[]
    {
        typeof(FleetIQ.BuildingBlocks.Application.ICommandHandler<,>),
        typeof(FleetIQ.BuildingBlocks.Application.IQueryHandler<,>)
    };
    foreach (var type in assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false }))
        foreach (var iface in type.GetInterfaces().Where(i => i.IsGenericType && openHandlerInterfaces.Contains(i.GetGenericTypeDefinition())))
            services.AddScoped(iface, type);
}
