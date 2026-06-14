using FleetIQ.Identity.Domain;

namespace FleetIQ.Identity.Application.Abstractions;

public interface IUserAccountRepository
{
    Task AddAsync(UserAccount entity, CancellationToken ct);
    Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken ct);
}
