using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Identity.Domain;

// NOTE: Duende IdentityServer sits in front of this service for OIDC/OAuth2 token issuance; this domain owns users, tenants and roles.
public enum UserAccountStatus { Pending = 0, Active = 1, Locked = 2 }

public static class UserAccountErrors
{
    public static readonly Error InvalidTransition =
        new("UserAccount.InvalidTransition", "The item is not in a state that allows this operation.");
}

/// <summary>UserAccount aggregate root.</summary>
public sealed class UserAccount : AggregateRoot<Guid>
{
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public Guid TenantId { get; private set; }
    public UserAccountStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    private UserAccount() { } // EF

    private UserAccount(Guid id, string email, string fullName, Guid tenantId) : base(id)
    {
        Email = email;
        FullName = fullName;
        TenantId = tenantId;
        Status = UserAccountStatus.Pending;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static UserAccount Create(string email, string fullName, Guid tenantId)
    {
        var entity = new UserAccount(Guid.NewGuid(), email, fullName, tenantId);
        entity.Raise(new Events.UserAccountCreatedDomainEvent(entity.Id));
        return entity;
    }

    public Result Activate()
    {
        if (Status != UserAccountStatus.Pending) return Result.Failure(UserAccountErrors.InvalidTransition);
        Status = UserAccountStatus.Active;
        Raise(new Events.UserAccountStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }
}
