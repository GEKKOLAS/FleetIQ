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

        db.PredictionRequests.AddRange(
            PredictionRequest.Create("ModelId 1", "PredictionType 1"),
            PredictionRequest.Create("ModelId 2", "PredictionType 2"),
            PredictionRequest.Create("ModelId 3", "PredictionType 3"));

        await db.SaveChangesAsync();
    }
}
