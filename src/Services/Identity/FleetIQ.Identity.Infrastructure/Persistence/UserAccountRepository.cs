using FleetIQ.Identity.Application.Abstractions;
using FleetIQ.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Identity.Infrastructure.Persistence;

public sealed class UserAccountRepository(IdentityDbContext db) : IUserAccountRepository
{
    public async Task AddAsync(UserAccount entity, CancellationToken ct) => await db.UserAccounts.AddAsync(entity, ct);

    public Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.UserAccounts.FirstOrDefaultAsync(x => x.Id == id, ct);
}
