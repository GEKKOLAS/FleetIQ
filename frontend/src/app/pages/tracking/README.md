# FleetIQ Control Tower

Run `npm start` from `frontend`, sign in, then open `/app/tracking`.

- `tracking.html` / `tracking.scss`: responsive operations workspace, search, status filters, telemetry connection status and shipment detail.
- `logistics-scene.ts`: orbitable Three.js schematic using shipment coordinates and OSRM route geometry. The overview fits the active network, with a narrower deck and a camera adapted to mobile screens. Select a truck or shipment card; use **Ver envío** to inspect the selected vehicle, **Etiquetas** to toggle location names, or the compass to restore the overview. Scroll/pinch to zoom and drag to orbit.
- `logistics-assets.ts`: reusable, locally authored trucks and distribution depots with loading bays, forecourts, corrugated panels, roof equipment, vehicle glazing, lamps, mirrors and wheel hubs. Repeated details use instanced meshes; unselected trucks use a lighter model. No remote 3D assets or Spline account are required for this implementation.
- `logistics-visuals.ts`: path ribbons, the bounded network deck, and screen-sized location/selection labels. Labels yield when they overlap and reappear as the camera changes. Solid routes follow received road geometry without smoothing; estimated connections remain dashed.
- The street map remains available through the Map toggle. Solid paths use OSRM road geometry; dashed 3D paths connect endpoints when road geometry is unavailable. Warehouse assets represent recorded endpoints, not surveyed buildings. The scene is a schematic, not terrain or a 3D street map.
- Without shipments, the scene explicitly displays a sample illustration. No sample shipments are mixed into API data. Missing GPS, speed and city information are displayed as unavailable.
- SignalR updates refresh shipment positions and reported speed. Truck models interpolate between received positions and face their last movement; the initial heading is inferred from origin/destination when available. A disconnected badge means the latest received positions may be stale. Counts cover the loaded page of up to 100 shipments.
- GPS-only updates reuse network geometry and vehicle models. Endpoints, route changes, or a position outside the current network bounds recalculate the projection. Selecting a shipment changes its model/highlight while preserving the user's camera.
- Three.js loads with the tracking route. Rendering runs outside Angular, pixel density is capped, reduced motion disables interpolation/pulsing, and owned geometry, instance buffers, label textures, shadows, controls, map and connections are cleaned up on navigation.

## Validation

Production build: `npm run build`.

Local Chrome smoke coverage used mocked shipments and route responses and a browser-only authentication-guard bypass: WebGL rendering, card/canvas selection, geographic paths and dashed fallback, GPS-only geometry reuse, missing-GPS suppression, map switching, resource cleanup, and desktop/390px mobile rendering. No production authentication changes were made. Live backend and OAuth integration require the running FleetIQ services.

## Spline integration

An editable [FleetIQ warehouse model](https://app.spline.design/file/4f70bf49-7bcc-441d-9270-f5209ebc08f3) now maps the app's order, load, planning, dispatch, and tracking processes into five interactive zones. Open its Preview tab for stage selection, a roof toggle, and a play/pause/reset process demonstration. Its goods, positions, and timing are illustrative; it has no live telemetry or backend commands.

The source and domain mapping are in [docs/spline](../../../../../docs/spline/README.md). This is a separate editable design asset; `/app/tracking` continues to render the Three.js scene described above.
