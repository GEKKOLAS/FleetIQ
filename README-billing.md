# Billing - complete reference service

This overlay replaces the lean Billing stub with a fully modeled DDD/CQRS service. Use it as the
template to flesh out the other services.

## What it now has
- **Rich aggregate** `Invoice` with `InvoiceLine` children, a `Money` value object, a real lifecycle
  (Draft -> Issued -> Paid, plus Cancel) and invariants enforced in the domain.
- **Intent-based commands**: CreateInvoice, AddInvoiceLine, IssueInvoice, RecordInvoicePayment, CancelInvoice.
- **Queries with DTOs**: GetInvoice (detail + lines) and **ListInvoices** (paged, filterable by customer/status/date, sortable).
- **ProblemDetails** (RFC 7807) error responses; validation via FluentValidation.
- **Domain unit tests** covering the lifecycle and invariants.

## Endpoints (through the gateway: http://localhost:5080)
| Method | Path | Purpose |
|--------|------|---------|
| POST   | /invoices | create (draft, with lines) |
| GET    | /invoices/{id} | full detail + lines |
| GET    | /invoices?customerId=&status=&from=&to=&page=&pageSize=&sort= | paged list |
| POST   | /invoices/{id}/lines | add a line (draft only) |
| POST   | /invoices/{id}/issue | issue (body: { "dueDateUtc": "..." }) |
| POST   | /invoices/{id}/payment | mark paid |
| POST   | /invoices/{id}/cancel | cancel |

## IMPORTANT: the schema changed -> recreate the Billing DB + migration
The old stub stored Amount/Currency as plain columns; the new model has a Total value object and an
invoice_lines table. So you must regenerate Billing's migration and recreate its database:

```powershell
# from the repo root, with the Billing service stopped
Remove-Item -Recurse -Force src/Services/Billing/FleetIQ.Billing.Infrastructure/Persistence/Migrations -ErrorAction SilentlyContinue

dotnet ef migrations add InitialCreate `
  --project src/Services/Billing/FleetIQ.Billing.Infrastructure `
  --startup-project src/Services/Billing/FleetIQ.Billing.Api `
  --output-dir Persistence/Migrations

# drop & recreate just the billing database (Postgres is in docker)
docker exec fleetiq-postgres psql -U fleetiq -d fleetiq -c "DROP DATABASE IF EXISTS fleetiq_billing;"
docker exec fleetiq-postgres psql -U fleetiq -d fleetiq -c "CREATE DATABASE fleetiq_billing;"
```

Then restart Billing (or the whole `run-all.ps1`). On startup it applies the migration and seeds a
draft + an issued invoice, so `GET /invoices` returns data immediately.
