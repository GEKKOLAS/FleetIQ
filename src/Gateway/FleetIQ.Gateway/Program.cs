using FleetIQ.BuildingBlocks.Web;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddFleetIqJwtAuth(builder.Configuration);
builder.Services.AddHealthChecks();

// Allow the Angular app (dev server + GitHub Pages) to call the gateway, including SignalR (credentials).
const string FrontendCors = "frontend";
builder.Services.AddCors(o => o.AddPolicy(FrontendCors, p => p.WithOrigins("http://localhost:4200", "https://gekkolas.github.io").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseCors(FrontendCors);
app.MapHealthChecks("/health");

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.Run();
