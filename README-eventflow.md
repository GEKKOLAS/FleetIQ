# Cross-service event flow: Billing -> Notifications

## What this adds
- **`src/Contracts/FleetIQ.Contracts`** - a shared assembly with integration-event contracts
  (`InvoiceIssuedIntegrationEvent`). Services publish/consume THESE, never each other's domain events.
- **`IIntegrationEventPublisher`** (BuildingBlocks) - keeps the Application layer free of MassTransit;
  the MassTransit implementation lives in BuildingBlocks.Infrastructure.
- **Billing** publishes `InvoiceIssuedIntegrationEvent` when an invoice is issued.
- **Notifications** has an `InvoiceIssuedConsumer` that creates a notification when it receives that event,
  plus a new **`GET /notifications`** list endpoint so you can see it.

## The flow
```
POST /invoices/{id}/issue  (Billing)
        |  invoice.Issue() succeeds, SaveChanges
        v  publisher.PublishAsync(InvoiceIssuedIntegrationEvent)
   RabbitMQ  (MassTransit)
        v
InvoiceIssuedConsumer  (Notifications)  -> creates a Notification
        v
GET /notifications  shows the new "invoice-issued" notification
```

## Apply
1. The new `FleetIQ.Contracts` project must be in the solution. Re-run:
   ```powershell
   .\scripts\init-solution.sh    # or: dotnet sln add src/Contracts/FleetIQ.Contracts/FleetIQ.Contracts.csproj
   ```
   (Even without that, project references pull it into the build.)
2. No DB changes needed (Notifications schema is unchanged).
3. Restart everything: `.\scriptsun-all.ps1`.

## Test (Postman)
1. `0 - Get Token`
2. `05 - Billing` -> `Create Invoice`  (saves invoiceId)
3. `05 - Billing` -> `Issue Invoice`
4. `08 - Notifications` -> `List Notifications`  -> you'll see a new "invoice-issued" row.
   (You can also watch the Notifications console window: it logs the consumed event.)
