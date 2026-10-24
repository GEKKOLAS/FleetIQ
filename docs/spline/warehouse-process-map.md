# FleetIQ warehouse process model

[Open the editable Spline model](https://app.spline.design/file/4f70bf49-7bcc-441d-9270-f5209ebc08f3). File UUID: `4f70bf49-7bcc-441d-9270-f5209ebc08f3`.

The warehouse is a conceptual spatial map of FleetIQ's order, planning, carrier, and tracking workflows. The application has no warehouse, inventory, storage-bin, picking, or stock-management aggregate. Racks, pallets, docks, and the yard illustrate transport processes; they do not represent measured facilities or inventory records.

## Spatial zones and workflow

| Zone | Application mapping | Constraints and handoff |
| --- | --- | --- |
| 01 · Pedidos | Order: `Draft → Confirmed → ReadyForPlanning` | Orders contain pickup/delivery addresses and items. Add items only while `Draft`; confirm before marking ready. The scene does not imply that readiness automatically creates a load. |
| 02 · Consolidación | Load: `Draft` | Group order references, descriptions, and weights into a load. Adding orders is restricted to `Draft`; `TotalWeightKg` sums their weights. Pallets illustrate these order lines. |
| 03 · Planificación | Route: `Draft → Finalized`; Load: `Draft → Planned` | Finalize a route with at least two stops (`Pickup` or `Delivery`). Attach a finalized route belonging to the same load. A load needs at least one order, an assigned verified carrier, and an attached route before planning. |
| 04 · Despacho | Load: `Planned → Confirmed`; Shipment: `Created → Dispatched` | Load confirmation publishes `LoadConfirmedIntegrationEvent`. Tracking consumes it, skips duplicate shipments for the load, then creates and immediately dispatches the shipment. |
| 05 · Seguimiento y entrega | Shipment: `Dispatched → InTransit → Delivered` | The first accepted location update changes `Dispatched` to `InTransit`. Delivery is allowed from either moving state and records recipient and delivery time. Exceptions add timeline events without introducing a shipment status. |

The normal path across these zones is a process explanation spanning separate entities, not one shared status machine. In particular, `Confirmed` means different things for an order and a load.

## Declared states and supporting entities

| Entity | Exact declared values |
| --- | --- |
| Order (4) | `Draft`, `Confirmed`, `ReadyForPlanning`, `Cancelled` |
| Load (4) | `Draft`, `Planned`, `Confirmed`, `Cancelled` |
| Route (2) | `Draft`, `Finalized` |
| Shipment (5) | `Created`, `Dispatched`, `InTransit`, `Delivered`, `Cancelled` |
| Carrier (3) | `Onboarding`, `Verified`, `Suspended` |
| Vehicle (3) | `Available`, `InUse`, `Maintenance` |
| Driver (3) | `Available`, `OnDuty`, `Off` |
| Tracking event (5) | `Created`, `Dispatched`, `Milestone`, `Exception`, `Delivered` |

A carrier needs at least one vehicle and one driver to become `Verified`; only verified carriers can be assigned through the planning application handler. Loads reference the carrier, not a specific vehicle or driver. The illustrated truck therefore represents carrier transport capacity, not an actual vehicle assignment.

An order cannot be cancelled after `ReadyForPlanning`; a load cannot be cancelled after `Confirmed`. `Cancelled` is declared for shipments, but the current shipment aggregate exposes no cancellation method. Frequent GPS updates replace the current position and speed; they do not each add a tracking-event row.

## Demonstration interactions

- Select a zone or stage button to highlight it and explain its application stage and prerequisites.
- Select the truck to inspect tracking and delivery; drag the scene to orbit and scroll to zoom.
- Toggle the roof to inspect the warehouse layout.
- Play or pause an illustrative process walkthrough, with the current stage highlighted.
- Reset the walkthrough, roof, cargo, truck, and dock shutter to their initial state. Reduced-motion preferences keep exploration available without automatic travel.

These interactions are a demonstration of the workflow. Scene positions, goods, timing, and any walkthrough values are illustrative; the Spline document is not connected to live backend data and does not execute application commands. Use a visible label such as **“Demostración de procesos · sin telemetría en vivo”**.

## Source references

- Orders: [Order.cs](../../src/Services/Orders/FleetIQ.Orders.Domain/Orders/Order.cs), [OrderStatus.cs](../../src/Services/Orders/FleetIQ.Orders.Domain/Orders/OrderStatus.cs).
- Loads and routes: [Load.cs](../../src/Services/Planning/FleetIQ.Planning.Domain/Load.cs), [Route.cs](../../src/Services/Planning/FleetIQ.Planning.Domain/Route.cs), [RouteStop.cs](../../src/Services/Planning/FleetIQ.Planning.Domain/RouteStop.cs).
- Planning validation and event publication: [AssignCarrierCommand.cs](../../src/Services/Planning/FleetIQ.Planning.Application/AssignCarrier/AssignCarrierCommand.cs), [AttachRouteCommand.cs](../../src/Services/Planning/FleetIQ.Planning.Application/AttachRoute/AttachRouteCommand.cs), [ConfirmLoadCommand.cs](../../src/Services/Planning/FleetIQ.Planning.Application/ConfirmLoad/ConfirmLoadCommand.cs).
- Planning-to-tracking handoff: [IntegrationEvents.cs](../../src/Contracts/FleetIQ.Contracts/IntegrationEvents.cs), [LoadConfirmedConsumer.cs](../../src/Services/Tracking/FleetIQ.Tracking.Infrastructure/Consumers/LoadConfirmedConsumer.cs).
- Shipment lifecycle and timeline: [Shipment.cs](../../src/Services/Tracking/FleetIQ.Tracking.Domain/Shipment.cs), [ShipmentStatus.cs](../../src/Services/Tracking/FleetIQ.Tracking.Domain/ShipmentStatus.cs), [TrackingEvent.cs](../../src/Services/Tracking/FleetIQ.Tracking.Domain/TrackingEvent.cs).
- Carrier resources: [Carrier.cs](../../src/Services/Carriers/FleetIQ.Carriers.Domain/Carrier.cs), [Vehicle.cs](../../src/Services/Carriers/FleetIQ.Carriers.Domain/Vehicle.cs), [Driver.cs](../../src/Services/Carriers/FleetIQ.Carriers.Domain/Driver.cs).
