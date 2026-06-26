# Orders - rich service

Replaces the lean Orders stub. `OrderItem` is a REGULAR entity (table `order_items`, FK), so adding
items to an existing order works (no owned-collection update bug).

## Endpoints (via gateway http://localhost:5080)
| Method | Path | Purpose |
|---|---|---|
| POST | /orders | create (with items) |
| GET | /orders/{id} | detail + items + addresses |
| GET | /orders?customerId=&status=&page=&pageSize=&sort= | paged list |
| POST | /orders/{id}/items | add an item (draft only) |
| POST | /orders/{id}/confirm | Draft -> Confirmed |
| POST | /orders/{id}/ready-for-planning | Confirmed -> ReadyForPlanning |
| POST | /orders/{id}/cancel | cancel |

## Schema changed -> recreate the Orders migration + DB (Orders stopped):
```powershell
Remove-Item -Recurse -Force src/Services/Orders/FleetIQ.Orders.Infrastructure/Persistence/Migrations -ErrorAction SilentlyContinue
dotnet ef migrations add InitialCreate --project src/Services/Orders/FleetIQ.Orders.Infrastructure --startup-project src/Services/Orders/FleetIQ.Orders.Api --output-dir Persistence/Migrations
docker exec fleetiq-postgres psql -U fleetiq -d fleetiq -c "DROP DATABASE IF EXISTS fleetiq_orders;"
docker exec fleetiq-postgres psql -U fleetiq -d fleetiq -c "CREATE DATABASE fleetiq_orders;"
```
Then restart Orders. Tracking and Planning follow this same pattern next.
