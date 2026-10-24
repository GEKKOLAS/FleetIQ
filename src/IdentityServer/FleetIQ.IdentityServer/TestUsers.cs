using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer.Test;

namespace FleetIQ.IdentityServer;

/// <summary>Dev-only in-memory users. Replace with ASP.NET Core Identity / a real store before production.</summary>
public static class TestUsers
{
    /// <param name="password">From configuration (IdentityServer:DemoUserPassword) - never hardcoded.</param>
    public static List<TestUser> Users(string password) =>
    [
        new TestUser
        {
            SubjectId = "1",
            Username = "alice",
            Password = password,
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
            Password = password,
            Claims =
            {
                new Claim(JwtClaimTypes.Name, "Bob Jones"),
                new Claim(JwtClaimTypes.Email, "bob@fleetiq.io")
            }
        }
    ];
}
