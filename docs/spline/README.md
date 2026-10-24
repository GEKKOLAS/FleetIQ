# FleetIQ interactive warehouse

[Open the editable Spline file](https://app.spline.design/file/4f70bf49-7bcc-441d-9270-f5209ebc08f3). File: `FleetIQ Control Tower`, UUID `4f70bf49-7bcc-441d-9270-f5209ebc08f3`.

The cutaway warehouse uses FleetIQ's navy/mint palette and maps five zones to the actual transport workflow: orders, consolidation, planning, dispatch, and tracking/delivery. It includes staged parcels, a control desk, loading portal, moving demonstration cargo, delivery truck, and a roof that can be shown or hidden.

In Spline's **Preview** tab, select a numbered zone or a process button to inspect a stage. Use the controls to show the roof, play or pause the 24-second walkthrough, and reset the scene. The truck selects tracking/delivery. Drag to orbit and scroll to zoom. Reduced-motion settings keep manual stage selection available while disabling automatic travel.

This is a process visualization with demonstration data. It does not call FleetIQ APIs, mutate orders/loads/shipments, manage warehouse stock, or display live GPS. The existing Angular tracking page still uses Three.js. See [warehouse-process-map.md](warehouse-process-map.md) for the domain rules and authoritative source references.

## Editable source

The numbered `.js` files are Spline editor DSL, not application JavaScript modules. They record the authored scene in dependency order. Use them in a new empty Spline file after loading the Spline authoring guides; rerunning creation batches in the existing document would add duplicate objects. Existing-file changes should target the stable object names instead.

1. `01-warehouse-shell.js`: site, warehouse shell, roof, shared materials, branding, and lighting.
2. `02-process-zones.js`: five selectable zones, illustrative goods, and demonstration cargo.
3. `03-docks-and-control.js`: control desk, dock, yard route, and safety props.
4. `04-delivery-truck.js`: editable truck components.
5. `05-organize-and-frame.js`: text sizing, named assemblies, and orbit controls.
6. `06-final-composition.js`: labels and the saved camera overview.

Attach [warehouse-interaction.html](warehouse-interaction.html) as the file's HTML content. It provides responsive Spanish controls and drives scene objects through Spline's native bridge. In this session, native canvas captures did not include the separate HTML HUD.

Key names are `FleetIQ Warehouse`, `Warehouse Roof`, `FleetIQ Delivery Truck`, `Demo Cargo`, `Dock Shutter`, and `Zone 1 Pad` / `Zone 1 Target` through zone 5. The HTML caches their snapshots and uses absolute transforms calculated from elapsed time. All interactive changes are transient and reset with the demonstration.

## Validation

Scene structure and named assemblies were read back from Spline, and the warehouse was visually reviewed in the editor and native Play mode. The HTML passed JavaScript syntax checks, a mocked-bridge interaction harness (stages, motion, completion, pause/reset, roof, reduced motion, and missing objects), and a Chrome check at 292px with no horizontal page overflow. Native canvas captures alone do not establish HTML-HUD execution, so exercise the controls in Preview when reviewing the connected runtime.

The Spline Free account reached its daily limit of 20 tool calls after saving the HTML controls and removing the temporary verification autoplay script. The model remains in edit mode, with 190 scene objects and all five zones grouped. This blocked a final camera adjustment and connected Preview verification. When the quota resets, open this same file, check the complete warehouse framing at the actual panel width, and exercise stage selection, roof visibility, play/pause/reset, cargo travel, and truck departure in Preview. The saved interaction document starts paused.
