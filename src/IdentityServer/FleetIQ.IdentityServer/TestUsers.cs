using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer.Test;

namespace FleetIQ.IdentityServer;

/// <summary>Dev-only in-memory users. Replace with ASP.NET Core Identity / a real store before production.</summary>
public static class TestUsers
{
    public static List<TestUser> Users =>
    [
        new TestUser
        {
            SubjectId = "1",
            Username = "alice",
            Password = "REDACTED",
            Claims =
            {
                new Claim(JwtClaimTypes.Name, "Alice Smith"),
                new Claim(JwtClaimTypes.Email, "alice@fleetiq.io")
            }
        },
        new TestUser
        {
            SubjectId = "2",
            Username = "bob",
            Password = "REDACTED",
            Claims =
            {
                new Claim(JwtClaimTypes.Name, "Bob Jones"),
                new Claim(JwtClaimTypes.Email, "bob@fleetiq.io")
            }
        }
    ];
}
