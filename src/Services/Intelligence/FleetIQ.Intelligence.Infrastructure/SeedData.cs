using FleetIQ.Intelligence.Domain;
using FleetIQ.Intelligence.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Intelligence.Infrastructure;

/// <summary>Inserts sample rows on first run (Development only).</summary>
public static class SeedData
{
    public static async Task SeedAsync(IntelligenceDbContext db)
    {
        if (await db.PredictionRequests.AnyAsync()) return;

        var eta = PredictionRequest.Create("eta-v2", "Eta").Value;
        eta.Start();
        eta.Complete("""{"etaUtc":"2026-07-04T16:30:00Z","routeKm":412}""", 0.87m);

        var delay = PredictionRequest.Create("delay-risk-v1", "Delay").Value;
        delay.Start();

        var price = PredictionRequest.Create("spot-price-v3", "Price").Value;

        db.PredictionRequests.AddRange(eta, delay, price);
        await db.SaveChangesAsync();
    }
}
