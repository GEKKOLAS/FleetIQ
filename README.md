# FleetIQ — Backend Monorepo Scaffold

Microservices on **ASP.NET Core (.NET 10)** with **Domain-Driven Design** and **Clean Architecture**.
This is the foundation skeleton every service clones from, plus a complete **Orders** reference service.

## Why a monorepo
One repository, one solution. You can refactor shared `BuildingBlocks` and service contracts in a single
atomic commit, CI is one pipeline, and onboarding is "clone and run." Each service is still isolated
(its own database, its own deployable API) so it can be extracted into its own repo later if a team takes
sole ownership.

## Layout
```
fleetiq/
  docker-compose.yml            # Postgres(+PostGIS) · RabbitMQ · Redis · MinIO
  Directory.Packages.props      # central NuGet versions
  src/
    BuildingBlocks/             # shared, reused by every service
      *.Domain                  # Entity, AggregateRoot, ValueObject, Result, IDomainEvent
      *.Application             # ICommand/IQuery + handler contracts, IUnitOfWork
      *.Infrastructure          # transactional Outbox + MassTransit publisher
    Services/
      Orders/                   # reference service — copy this for the next context
        *.Domain                # Order aggregate, value objects, domain events
        *.Application           # CreateOrder command, GetOrder query, validators
        *.Infrastructure        # EF Core (Npgsql), repository, DbContext, DI
        *.Api                   # Minimal API endpoints, health, OpenAPI, OTel
  tests/
    FleetIQ.Orders.UnitTests    # xUnit domain tests
```

## Dependency rule (Clean Architecture)
`Api -> Infrastructure -> Application -> Domain`. Domain depends on nothing.
Application defines interfaces (e.g. `IOrderRepository`); Infrastructure implements them.

## Getting started
```bash
cp .env.example .env
make up                 # start Postgres, RabbitMQ, Redis, MinIO
make init               # create FleetIQ.sln and add all projects

# create the first EF migration for Orders, then run the API
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate \
  --project src/Services/Orders/FleetIQ.Orders.Infrastructure \
  --startup-project src/Services/Orders/FleetIQ.Orders.Api

make run-orders         # https://localhost:xxxx/openapi  ·  /health
make test
```

## Try the API
```bash
curl -X POST http://localhost:5080/orders -H "Content-Type: application/json" -d '{
  "customerId": "11111111-1111-1111-1111-111111111111",
  "pickup":   {"street":"1 Main St","city":"Lisbon","postalCode":"1000-001","country":"PT"},
  "delivery": {"street":"5 Park Ave","city":"Porto","postalCode":"4000-002","country":"PT"},
  "items":    [{"description":"Pallet","quantity":2,"weightKg":120}]
}'
```

## Adding the next service (e.g. Carriers)
1. Copy `src/Services/Orders` -> `src/Services/Carriers`, rename namespaces/projects.
2. Model the aggregate(s) in `*.Domain` (see the Architecture Map for each context's building blocks).
3. Add its database to `docker-compose.yml` and a connection string.
4. `dotnet sln add` the new projects (or re-run `make init`).

## Patterns already wired
- **CQRS** via dependency-free `ICommand/IQuery` handlers (no MediatR -> no license risk).
- **Transactional Outbox**: domain events are saved in the same transaction as the aggregate and
  published to RabbitMQ by `OutboxProcessor<TContext>` — reliable, exactly-once-ish event publishing.
- **Result type** so the domain returns rule violations instead of throwing.
- **Observability**: Serilog + OpenTelemetry (OTLP) + health checks out of the box.

## Library licensing notes (verified June 2026)
- **MassTransit v8** (used here) is free under Apache-2.0 but loses support at end of 2026.
  v9 (2026) is commercial above ~$1M revenue. Alternative: **Wolverine** (MIT) replaces both the
  mediator and the bus.
- **MediatR / AutoMapper** went commercial in 2025 (free under $5M). This scaffold avoids both:
  a hand-rolled mediator and no object-mapper (map by hand or add **Mapperly**, source-generated, MIT).
- **Duende IdentityServer** (added with the Identity service later) is free under $1M revenue;
  **OpenIddict** is the free fallback.

> Package versions in `Directory.Packages.props` are pinned to plausible .NET 10 releases — run
> `dotnet restore` and bump to the latest patch as needed.
