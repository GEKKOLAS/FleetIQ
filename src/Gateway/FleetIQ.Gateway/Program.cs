using FleetIQ.BuildingBlocks.Web;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddFleetIqJwtAuth(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.MapHealthChecks("/health");          // anonymous

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();                    // every proxied route requires an authenticated caller

app.Run();
