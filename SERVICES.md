# FleetIQ Services Index

All 12 bounded-context services in the monorepo. Each has its own database (database-per-service), its own 4-layer projects (Domain / Application / Infrastructure / Api), and publishes domain events via the shared transactional outbox to RabbitMQ.

| # | Service | Aggregate | Schema / DB | Route |
|---|---------|-----------|-------------|-------|
| 1 | Orders | Order | fleetiq_orders | /orders |
| 2 | Identity | UserAccount | fleetiq_identity | /useraccounts |
| 3 | Carriers | Carrier | fleetiq_carriers | /carriers |
| 4 | Planning | Load | fleetiq_planning | /loads |
| 5 | Tracking | Shipment | fleetiq_tracking | /shipments |
| 6 | Billing | Invoice | fleetiq_billing | /invoices |
| 7 | Documents | Document | fleetiq_documents | /documents |
| 8 | Notifications | Notification | fleetiq_notifications | /notifications |
| 9 | Analytics | ReportDefinition | fleetiq_analytics | /reportdefinitions |
| 10 | Compliance | ComplianceCheck | fleetiq_compliance | /compliancechecks |
| 11 | Integration | IntegrationConnector | fleetiq_integration | /integrationconnectors |
| 12 | Intelligence | PredictionRequest | fleetiq_intelligence | /predictionrequests |

## Status of each service

- **Orders** is the fully-modeled exemplar (value objects, child entities, owned types).
- **Carriers, Planning, Tracking, Billing** are deepened: child entities, full state machines, cross-service event flows (e.g. InvoiceIssued -> Notifications).
- **Documents, Notifications, Analytics, Compliance, Integration, Intelligence** are deepened (2026-07-03):
  - *Documents*: versions (child), expiry, Uploaded -> Verified | Rejected -> re-upload -> Archived.
  - *Notifications*: validated channel, Pending -> Sent | Failed(+reason) -> Retry (max 5 attempts).
  - *Analytics*: schedule (OnDemand/Daily/Weekly/Monthly) + ReportRun history (child).
  - *Compliance*: findings (child) with severity; Pass blocked by High/Critical findings; Fail requires findings; Waive with reason.
  - *Integration*: endpoint URL validation, sync health, auto-disable after 3 consecutive failures.
  - *Intelligence*: Requested -> Running -> Completed(result+confidence 0..1) | Failed(+error).
  All have `GET /` list endpoints, richer detail responses, `DeepenDomain` EF migrations and domain unit tests (46 green).
- **Identity** remains a lean scaffold (auth itself lives in Duende IdentityServer).

## Run a service
```bash
make up   # starts Postgres (creates all DBs), RabbitMQ, Redis, MinIO
make init # builds the solution file

# create the first migration for a service, e.g. Carriers:
dotnet ef migrations add InitialCreate \\
  --project src/Services/Carriers/FleetIQ.Carriers.Infrastructure \\
  --startup-project src/Services/Carriers/FleetIQ.Carriers.Api

dotnet run --project src/Services/Carriers/FleetIQ.Carriers.Api
```
