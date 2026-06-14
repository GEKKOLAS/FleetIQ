# FleetIQ Dev Tools

Drop these into the repo root (the `scripts/` folder merges with the existing one).

## 1. Migrations (schema only - no data needed)
A migration creates the TABLES from the EF model. The databases are already created
empty by `docker compose` (one per service). Run once:

```powershell
./scripts/add-migrations.ps1      # Windows
./scripts/add-migrations.sh       # macOS/Linux
```

## 2. Run EVERYTHING at once (for front-end work)
```powershell
docker compose up -d              # infra: Postgres (+all DBs), RabbitMQ, Redis, MinIO
./scripts/run-all.ps1             # IdentityServer + Gateway + all 12 services
```
- Front-end entry point (gateway): **http://localhost:5080**
- Token endpoint: **https://localhost:5001/connect/token**
- Each service applies its migration automatically on first start.

## 3. Postman
Import `FleetIQ.postman_collection.json`.
1. Run **"0 - Get Token"** first (saves the bearer token automatically).
2. Then any service: **Create** saves the new id into a variable, which **Get** / transition reuse.
3. In Postman *Settings -> General*, turn **off** "SSL certificate verification" (local dev cert).

All requests go through the gateway, so this is exactly the surface your front-end will use.

## Optional: seed data
Migrations don't seed data. If you want sample rows, add a small seeder in each service's
`Program.cs` (Development only) or a SQL script in `scripts/postgres-init/`. Ask and I'll generate it.
