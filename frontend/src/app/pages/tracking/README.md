# FleetIQ Control Tower

Run `npm start` from `frontend`, sign in, then open `/app/tracking`.

- `tracking.html` / `tracking.scss`: responsive operations workspace, search, status filters, telemetry connection status and shipment detail.
- `logistics-scene.ts`: orbitable Three.js schematic using shipment coordinates and OSRM route geometry. Select a truck or a shipment card; scroll to zoom and drag to orbit. The compass resets the camera.
- `logistics-assets.ts`: reusable, locally authored truck and warehouse meshes. No remote 3D assets or Spline account are required for this implementation.
- The street map remains available through the Map toggle. Solid paths use OSRM road geometry; dashed 3D paths connect endpoints when road geometry is unavailable. Warehouse assets represent recorded endpoints, not surveyed buildings. The scene is a schematic, not terrain or a 3D street map.
- Without shipments, the scene explicitly displays a sample illustration. No sample shipments are mixed into API data. Missing GPS, speed and city information are displayed as unavailable.
- SignalR updates refresh shipment positions and reported speed. A disconnected badge means the latest received positions may be stale. Counts cover the loaded page of up to 100 shipments.
- Three.js loads with the tracking route. Rendering runs outside Angular, pixel density is capped, and GPU resources, controls, map and connections are cleaned up on navigation.

## Validation

Production build: `npm run build`.

Local Chrome smoke coverage used mocked shipments and route responses and a browser-only authentication-guard bypass: WebGL rendering, selection, search, status filtering, map markers, view switching, mobile overflow, dark theme, and empty/error states. No production authentication changes were made. Live backend and OAuth integration require the running FleetIQ services.

## Spline integration

The Spline plugin was not exposed in the authoring session. To author editable Spline documents, connect [Spline](https://chatgpt.com/plugins/spline) with your account using the [official setup](https://docs.spline.design/generate/plugins/chatgpt). This implementation does not claim to create a Spline file.
