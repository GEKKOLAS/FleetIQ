using FleetIQ.Intelligence.Domain;
using FleetIQ.Intelligence.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Intelligence.Infrastructure;

/// <summary>Seeds prediction requests in every state with realistic models and results.</summary>
public static class SeedData
{
    public static async Task SeedAsync(IntelligenceDbContext db)
    {
        if (await db.PredictionRequests.AnyAsync()) return;

        var eta = PredictionRequest.Create("eta-v2", "Eta").Value;
        eta.Start();
        eta.Complete("""{"etaUtc":"2026-07-04T16:30:00Z","routeKm":412}""", 0.87m);

        var price = PredictionRequest.Create("spot-price-v3", "Price").Value;
        price.Start();
        price.Complete("""{"suggestedEur":1240,"corridor":"Madrid-Sevilla"}""", 0.91m);

        var delay = PredictionRequest.Create("delay-risk-v1", "Delay").Value;
        delay.Start();
        delay.Fail("Insufficient telemetry history for this carrier (< 30 trips)");

        var anomaly = PredictionRequest.Create("anomaly-detector-v1", "Anomaly").Value;
        anomaly.Start(); // running

        var etaQueue = PredictionRequest.Create("eta-v2", "Eta").Value;

        var priceQueue = PredictionRequest.Create("spot-price-v3", "Price").Value;

        db.PredictionRequests.AddRange(eta, price, delay, anomaly, etaQueue, priceQueue);
        await db.SaveChangesAsync();
    }
}
