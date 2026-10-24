# FleetIQ Platform Foundation

This adds the edge + auth + CI layer on top of the services.

## Components
- **IdentityServer** (`src/IdentityServer`) — Duende IdentityServer issuing OIDC/OAuth2 tokens. Dev config is in-memory.
- **Gateway** (`src/Gateway`) — YARP reverse proxy. Single entry point; every proxied route requires an authenticated caller.
- **BuildingBlocks.Web** — shared `AddFleetIqJwtAuth()` so all services validate tokens identically.
- **CI** (`.github/workflows/ci.yml`) — builds the solution and runs all tests on push/PR.

## Auth flow
```
client → IdentityServer (get token) → Gateway (validate) → Service (validate again) 
```
Both the gateway and each service validate the JWT (defense in depth).

## Local dev ports

| Service | Port | Gateway path |
|---------|------|--------------|
| Gateway | 5080 | (entry point) |
| IdentityServer | 5001 | n/a |
| Orders | 5081 | /orders |
| Identity | 5082 | /useraccounts |
| Carriers | 5083 | /carriers |
| Planning | 5084 | /loads |
| Tracking | 5085 | /shipments |
| Billing | 5086 | /invoices |
| Documents | 5087 | /documents |
| Notifications | 5088 | /notifications |
| Analytics | 5089 | /reportdefinitions |
| Compliance | 5090 | /compliancechecks |
| Integration | 5091 | /integrationconnectors |
| Intelligence | 5092 | /predictionrequests |

Set a service's URL with `ASPNETCORE_URLS`, e.g. `ASPNETCORE_URLS=https://localhost:5083 dotnet run --project src/Services/Carriers/FleetIQ.Carriers.Api`.

## Try it end-to-end
```bash
make up                                   # infra (Postgres creates all DBs, RabbitMQ, Redis, MinIO)
make init                                 # build solution file

# 1. run the token server
dotnet run --project src/IdentityServer/FleetIQ.IdentityServer      # https://localhost:5001

# 2. run a service on its gateway port
ASPNETCORE_URLS=https://localhost:5083 dotnet run --project src/Services/Carriers/FleetIQ.Carriers.Api

# 3. run the gateway
ASPNETCORE_URLS=http://localhost:5080 dotnet run --project src/Gateway/FleetIQ.Gateway

# 4. get a service token
TOKEN=$(curl -ks -X POST https://localhost:5001/connect/token \\
  -d "grant_type=client_credentials&client_id=fleetiq.service&client_secret=$FLEETIQ_SERVICE_CLIENT_SECRET&scope=fleetiq.api" \\
  | python3 -c "import sys,json;print(json.load(sys.stdin)['access_token'])")

# 5. call a service THROUGH the gateway with the bearer token
curl -k -X POST http://localhost:5080/carriers \\
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \\
  -d '{"legalName":"Acme Freight","registrationNumber":"PT12345","country":"PT"}'
```

> Package versions for Yarp, JwtBearer and Duende.IdentityServer in `Directory.Packages.props` are pinned to plausible .NET 10 releases — run `dotnet restore` and bump to the latest patch as needed. Duende IdentityServer is free under the revenue threshold; OpenIddict is the free fallback.
