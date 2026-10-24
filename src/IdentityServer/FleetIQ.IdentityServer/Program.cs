using FleetIQ.IdentityServer;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

builder.Services.AddRazorPages(); // interactive login UI

// Pin the issuer so tokens stay valid for the services (Authority=https://localhost:5001) even when
// the IdP is reached through a public tunnel host (see scripts/start-public-demo.ps1).
builder.Services.AddIdentityServer(o => o.IssuerUri = builder.Configuration["IdentityServer:IssuerUri"] ?? "https://localhost:5001")
    .AddInMemoryIdentityResources(Config.IdentityResources)
    .AddInMemoryApiScopes(Config.ApiScopes)
    .AddInMemoryApiResources(Config.ApiResources)
    .AddInMemoryClients(Config.Clients)
    .AddTestUsers(TestUsers.Users)
    .AddDeveloperSigningCredential();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseStaticFiles();
app.UseRouting();

app.UseIdentityServer();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
