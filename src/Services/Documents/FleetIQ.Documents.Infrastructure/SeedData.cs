using FleetIQ.Documents.Domain;
using FleetIQ.Documents.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Documents.Infrastructure;

/// <summary>Inserts sample rows on first run (Development only).</summary>
public static class SeedData
{
    public static async Task SeedAsync(DocumentsDbContext db)
    {
        if (await db.Documents.AnyAsync()) return;

        db.Documents.AddRange(
            Document.Create(Guid.NewGuid(), "Category 1", "StorageRef 1"),
            Document.Create(Guid.NewGuid(), "Category 2", "StorageRef 2"),
            Document.Create(Guid.NewGuid(), "Category 3", "StorageRef 3"));

        await db.SaveChangesAsync();
    }
}
