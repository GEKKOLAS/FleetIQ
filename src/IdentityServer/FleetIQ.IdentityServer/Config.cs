using System.Security.Cryptography;
using System.Text;
using Duende.IdentityServer.Models;

namespace FleetIQ.IdentityServer;

/// <summary>In-memory configuration for local development. Move to a persisted store before production.</summary>
public static class Config
{
    // Secrets must be stored as Base64(SHA-256) so Duende's HashedSharedSecretValidator recognizes them.
    private static string Sha256(string input)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(input)));

    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile()
    ];

    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope("fleetiq.api", "FleetIQ APIs")
    ];

    public static IEnumerable<ApiResource> ApiResources =>
    [
        new ApiResource("fleetiq.api") { Scopes = { "fleetiq.api" } }
    ];

    public static IEnumerable<Client> Clients =>
    [
        // Service-to-service (client credentials)
        new Client
        {
            ClientId = "fleetiq.service",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret(Sha256("REDACTED")) },
            AllowedScopes = { "fleetiq.api" }
        },
        // SPA / mobile (Authorization Code + PKCE) - for the Angular/Next.js clients later
        new Client
        {
            ClientId = "fleetiq.spa",
            AllowedGrantTypes = GrantTypes.Code,
            RequireClientSecret = false,
            RequirePkce = true,
            AllowedScopes = { "openid", "profile", "fleetiq.api" },
            RedirectUris = { "http://localhost:4200/callback" },
            PostLogoutRedirectUris = { "http://localhost:4200" },
            AllowedCorsOrigins = { "http://localhost:4200" }
        }
    ];
}