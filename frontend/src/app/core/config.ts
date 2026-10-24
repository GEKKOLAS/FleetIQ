// Central front-end config. Points at the FleetIQ gateway and the IdentityServer.
// Defaults target the local stack; a deployed build overrides them at startup from `config.json`
// (written by .github/workflows/pages.yml from deploy/pages-config.json).
export let API_BASE = 'http://localhost:5080';          // YARP gateway
export let IDP_AUTHORITY = 'https://localhost:5001';    // Duende IdentityServer
export const SPA_CLIENT_ID = 'fleetiq.spa';
export const SPA_SCOPE = 'openid profile fleetiq.api';

/** Loads `config.json` next to index.html, if present. Must run before bootstrap. */
export async function loadRuntimeConfig(): Promise<void> {
    try {
        const res = await fetch('config.json', { cache: 'no-store' });
        if (!res.ok) return;
        const cfg: { apiBase?: string; idpAuthority?: string } = await res.json();
        if (cfg.apiBase) API_BASE = cfg.apiBase.replace(/\/$/, '');
        if (cfg.idpAuthority) IDP_AUTHORITY = cfg.idpAuthority.replace(/\/$/, '');
    } catch {
        // no config.json (local dev) -> keep the defaults
    }
}
