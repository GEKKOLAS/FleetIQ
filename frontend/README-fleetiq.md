# FleetIQ frontend (Angular 20 + PrimeNG Sakai)

MVP web app built on the Sakai template, talking to the FleetIQ gateway with OIDC auth.

## What's wired
- **Auth (OIDC):** `angular-auth-oidc-client` against Duende IdentityServer (`https://localhost:5001`),
  client `fleetiq.spa`, Authorization Code + PKCE. Token is auto-attached to gateway calls.
- **API services:** `OrdersService` and `BillingService` hit the gateway (`http://localhost:5080`).
- **Pages:** Landing (`/`), Dashboard (`/app`), Orders (`/app/orders`), Billing (`/app/billing`).
- Config lives in `src/app/core/config.ts` (gateway URL, IdP authority, client id, scope).

## Run
```bash
cd frontend
npm install            # installs deps incl. angular-auth-oidc-client
npm start              # ng serve -> http://localhost:4200
```
Prereqs running: `docker compose up -d`, IdentityServer (5001), Gateway (5080), and the
services you'll use (Orders 5081, Billing 5086). Trust the dev cert (`dotnet dev-certs https --trust`).

## ✅ Auth flow works end-to-end (verified 2026-07-03)
IdentityServer has a login page (`Account/Login`) + logout and test users **alice / bob**
(password: `FLEETIQ_DEMO_USER_PASSWORD` in your local `.env`, see `scripts/setup-secrets.ps1`). Full flow verified: Landing → Enter app → IdP login → Dashboard, plus
deep links / full reloads on `/app/*` while logged in.

Two OIDC config fixes were needed on the Angular side:
- `postLoginRoute: '/app'` in `provideAuth` (`src/app.config.ts`) — the library navigates to its
  `postLoginRoute` (default `/`) after processing the callback, which was bouncing users back to
  the landing page after login.
- `authGuard` now delegates to the library's `autoLoginPartialRoutesGuard`
  (`src/app/core/auth.guard.ts`) — it validates tokens synchronously from storage, so deep links
  don't race the initial `checkAuth()` and bounce to the IdP. (A custom guard calling
  `oidc.checkAuth()` deadlocks when it runs during the callback's own checkAuth pipeline.)

## Notes
- `angular-auth-oidc-client` is pinned to `^20.0.0`; if `npm install` can't resolve it, use the latest
  v20 it offers.
- The default customer id in the create dialogs is the seeded one
  (`22222222-2222-2222-2222-222222222222`) so lists/filters line up with the backend seed data.
