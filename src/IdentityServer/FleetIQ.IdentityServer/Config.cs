using Duende.IdentityServer.Models;
using System.Collections.Generic;

namespace FleetIQ.IdentityServer;

/// <summary>In-memory configuration for local development. Move to a persisted store before production.</summary>
public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources => new List<IdentityResource>
    {
        new IdentityResources.OpenId(),
        new IdentityResources.Profile()
    };

    public static IEnumerable<ApiScope> ApiScopes => new List<ApiScope>
    {
        new ApiScope("fleetiq.api", "FleetIQ APIs")
    };

    public static IEnumerable<ApiResource> ApiResources => new List<ApiResource>
    {
        new ApiResource("fleetiq.api") { Scopes = { "fleetiq.api" } }
    };

    public static IEnumerable<Client> Clients => new List<Client>
    {
        // Service-to-service (client credentials)
        new Client
        {
            ClientId = "fleetiq.service",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret("REDACTED") },
            AllowedScopes = { "fleetiq.api" }
        },
        // SPA / mobile (Authorization Code + PKCE) — for the Angular/Next.js clients later
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
    };
}
