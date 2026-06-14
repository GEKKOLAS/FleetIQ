using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Identity.Application.Abstractions;

namespace FleetIQ.Identity.Application.GetUserAccount;

public sealed record GetUserAccountQuery(Guid Id) : IQuery<UserAccountResponse?>;

public sealed record UserAccountResponse(Guid Id, string Status, DateTime CreatedOnUtc);

public sealed class GetUserAccountQueryHandler(IUserAccountRepository repository)
    : IQueryHandler<GetUserAccountQuery, UserAccountResponse?>
{
    public async Task<UserAccountResponse?> Handle(GetUserAccountQuery query, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(query.Id, ct);
        return entity is null ? null : new UserAccountResponse(entity.Id, entity.Status.ToString(), entity.CreatedOnUtc);
    }
}
