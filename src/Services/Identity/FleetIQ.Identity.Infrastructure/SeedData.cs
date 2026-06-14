using FleetIQ.Identity.Domain;
using FleetIQ.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Identity.Infrastructure;

/// <summary>Inserts sample rows on first run (Development only).</summary>
public static class SeedData
{
    public static async Task SeedAsync(IdentityDbContext db)
    {
        if (await db.UserAccounts.AnyAsync()) return;

        db.UserAccounts.AddRange(
            UserAccount.Create("Email 1", "FullName 1", Guid.NewGuid()),
            UserAccount.Create("Email 2", "FullName 2", Guid.NewGuid()),
            UserAccount.Create("Email 3", "FullName 3", Guid.NewGuid()));

        await db.SaveChangesAsync();
    }
}
