import { CommonModule } from '@angular/common';
import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ToolbarModule } from 'primeng/toolbar';
import { firstValueFrom } from 'rxjs';
import * as L from 'leaflet';
import * as signalR from '@microsoft/signalr';
import { API_BASE } from '../../core/config';
import { ShipmentLocationMessage, ShipmentSummary } from '../../core/models';
import { TrackingService } from '../../core/tracking.service';
import { LogisticsScene, RouteCoordinates } from './logistics-scene';

type Sev = 'success' | 'info' | 'warn' | 'danger' | 'secondary';

@Component({
    selector: 'app-tracking',
    standalone: true,
    imports: [CommonModule, FormsModule, ButtonModule, TableModule, TagModule, ToastModule, ToolbarModule, LogisticsScene],
    providers: [MessageService],
    templateUrl: './tracking.html',
    styleUrl: './tracking.scss'
})
export class Tracking implements OnInit, AfterViewInit, OnDestroy {
    @ViewChild('mapEl') mapEl!: ElementRef<HTMLDivElement>;

    private api = inject(TrackingService);
    private oidc = inject(OidcSecurityService);
    private toast = inject(MessageService);

    private map?: L.Map;
    private markers = new Map<string, L.Marker>();
    private lastPos = new Map<string, [number, number]>();
    private routeLayers = new Map<string, L.Polyline>();
    private routeRequested = new Set<string>();
    private connection?: signalR.HubConnection;

    // Stable, visually distinct palette; each shipment gets one by hashing its id.
    private palette = ['#2563EB', '#DC2626', '#16A34A', '#D97706', '#7C3AED', '#0891B2', '#DB2777', '#65A30D', '#EA580C', '#0D9488'];

    shipments: ShipmentSummary[] = [];
    connected = false;
    creating = false;
    loading = false;
    loadError = false;
    view: 'map' | '3d' = '3d';
    query = '';
    statusFilter = 'all';
    selectedId?: string;
    routes: RouteCoordinates = {};
    speeds: Record<string, number | null> = {};
    private destroyed = false;
    private routeAbort = new AbortController();
    private mapResize?: ResizeObserver;

    get selectedShipment() { return this.shipments.find(s => s.id === this.selectedId); }
    get inTransit() { return this.shipments.filter(s => s.status === 'InTransit').length; }
    get delivered() { return this.shipments.filter(s => s.status === 'Delivered').length; }
    get positioned() { return this.shipments.filter(s => this.hasPosition(s)).length; }
    get filteredShipments() {
        const query = this.query.toLocaleLowerCase();
        return this.shipments.filter(s => (this.statusFilter === 'all' || s.status === this.statusFilter) && `${s.id} ${s.originCity ?? ''} ${s.destinationCity ?? ''}`.toLocaleLowerCase().includes(query));
    }

    hasPosition(s: ShipmentSummary) { return s.currentLatitude != null && s.currentLongitude != null && Number.isFinite(s.currentLatitude) && Number.isFinite(s.currentLongitude); }
    label(status: string) { return ({ InTransit: 'En tránsito', Delivered: 'Entregado', Dispatched: 'Despachado', Created: 'Creado', Cancelled: 'Cancelado' } as Record<string, string>)[status] ?? status; }

    setView(view: 'map' | '3d') { this.view = view; requestAnimationFrame(() => this.map?.invalidateSize()); }
    selectById(id: string) { const shipment = this.shipments.find(s => s.id === id); if (shipment) this.focus(shipment); }
    fitFleet() {
        const points = this.shipments.filter(s => this.hasPosition(s)).map(s => L.latLng(s.currentLatitude!, s.currentLongitude!));
        if (points.length) this.map?.fitBounds(L.latLngBounds(points), { padding: [48, 48], maxZoom: 12 });
    }

    ngOnInit() {
        // Load after the map is ready so synchronous responses also create markers.
    }

    ngAfterViewInit() {
        this.map = L.map(this.mapEl.nativeElement).setView([39.5, -8.0], 7);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { attribution: '© OpenStreetMap', maxZoom: 19 }).addTo(this.map);
        this.mapResize = new ResizeObserver(() => this.map?.invalidateSize());
        this.mapResize.observe(this.mapEl.nativeElement);
        this.load();
        this.connect();
    }

    load() {
        if (this.loading) return;
        this.loading = true;
        this.loadError = false;
        this.api.list({ pageSize: 100 }).subscribe({
            next: (p) => {
                if (this.destroyed) return;
                this.loading = false;
                const firstLoad = !this.shipments.length;
                this.shipments = p.items;
                const ids = new Set(p.items.map(s => s.id));
                for (const [id, marker] of this.markers) if (!ids.has(id)) { marker.remove(); this.markers.delete(id); this.lastPos.delete(id); }
                for (const [id, line] of this.routeLayers) if (!ids.has(id)) { line.remove(); this.routeLayers.delete(id); this.routeRequested.delete(id); delete this.routes[id]; }
                this.routes = { ...this.routes };
                if (!this.selectedId || !ids.has(this.selectedId)) this.selectedId = p.items[0]?.id;
                p.items.forEach((s) => {
                    this.upsert(s.id, s.currentLatitude, s.currentLongitude, s.status);
                    this.drawRoute(s);
                });
                if (firstLoad) this.fitFleet();
            },
            error: () => {
                this.loading = false;
                this.loadError = true;
                this.toast.add({ severity: 'error', summary: 'Error', detail: 'No se pudieron cargar los envíos' });
            }
        });
    }

    private async connect() {
        if (this.destroyed) return;
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(`${API_BASE}/hubs/tracking`, { accessTokenFactory: () => firstValueFrom(this.oidc.getAccessToken()) })
            .withAutomaticReconnect()
            .build();

        this.connection.on('ShipmentLocation', (m: ShipmentLocationMessage) => {
            this.upsert(m.shipmentId, m.latitude, m.longitude, m.status);
            const s = this.shipments.find((x) => x.id === m.shipmentId);
            if (s) {
                s.currentLatitude = m.latitude;
                s.currentLongitude = m.longitude;
                s.status = m.status;
                s.lastPositionAtUtc = m.atUtc;
                this.speeds = { ...this.speeds, [s.id]: m.speedKph ?? null };
                this.shipments = [...this.shipments];
            }
        });
        this.connection.onreconnecting(() => (this.connected = false));
        this.connection.onreconnected(() => (this.connected = true));
        this.connection.onclose(() => (this.connected = false));

        try {
            await this.connection.start();
            this.connected = true;
        } catch {
            this.connected = false;
        }
    }

    private upsert(id: string, lat?: number | null, lng?: number | null, status?: string) {
        if (lat == null || lng == null || !Number.isFinite(lat) || !Number.isFinite(lng) || !this.map) return;

        const last = this.lastPos.get(id);
        const heading = last ? this.bearing(last, [lat, lng]) : 0;
        this.lastPos.set(id, [lat, lng]);

        const color = this.colorFor(id);
        const existing = this.markers.get(id);
        if (existing) {
            existing.setLatLng([lat, lng]);
            this.rotate(existing, heading);
        } else {
            const m = L.marker([lat, lng], { icon: this.carIcon(color) }).addTo(this.map);
            m.bindTooltip(id.substring(0, 8));
            m.on('click', () => this.selectById(id));
            this.markers.set(id, m);
            // let the marker glide smoothly between 1s position updates
            const el = m.getElement();
            if (el) el.style.transition = 'transform 1s linear';
            this.rotate(m, heading);
        }
    }

    // Fetch the real driving route (OSRM) for a shipment and draw it as a colored road line.
    private async drawRoute(s: ShipmentSummary) {
        if (!this.map || this.routeRequested.has(s.id)) return;
        const oLat = s.originLatitude, oLng = s.originLongitude, dLat = s.destinationLatitude, dLng = s.destinationLongitude;
        if (oLat == null || oLng == null || dLat == null || dLng == null) return;
        if ((!oLat && !oLng) || (!dLat && !dLng)) return; // no real origin/destination
        this.routeRequested.add(s.id);
        try {
            const url = `https://router.project-osrm.org/route/v1/driving/${oLng},${oLat};${dLng},${dLat}?overview=full&geometries=geojson`;
            const res = await fetch(url, { signal: this.routeAbort.signal });
            if (!res.ok) throw new Error('Route unavailable');
            const data = await res.json();
            const coords: [number, number][] = (data?.routes?.[0]?.geometry?.coordinates ?? []).map((c: number[]) => [c[1], c[0]]);
            if (coords.length < 2 || !this.map || this.destroyed || !this.shipments.some(item => item.id === s.id)) {
                this.routeRequested.delete(s.id);
                return;
            }
            const color = this.colorFor(s.id);
            const line = L.polyline(coords, { color, weight: 4, opacity: 0.5, dashArray: '1 8', lineCap: 'round' }).addTo(this.map);
            line.bringToBack();
            this.routeLayers.set(s.id, line);
            this.routes = { ...this.routes, [s.id]: coords };
        } catch {
            this.routeRequested.delete(s.id); // allow a retry on next refresh
        }
    }

    // Stable color per shipment id (hash -> palette index).
    colorFor(id: string): string {
        let h = 0;
        for (let i = 0; i < id.length; i++) h = (h * 31 + id.charCodeAt(i)) >>> 0;
        return this.palette[h % this.palette.length];
    }

    private rotate(marker: L.Marker, deg: number) {
        const car = marker.getElement()?.querySelector('.fleet-car') as HTMLElement | null;
        if (car) car.style.transform = `rotate(${deg}deg)`;
    }

    private bearing(from: [number, number], to: [number, number]): number {
        const toRad = (d: number) => (d * Math.PI) / 180;
        const [lat1, lon1] = from, [lat2, lon2] = to;
        const dLon = toRad(lon2 - lon1);
        const y = Math.sin(dLon) * Math.cos(toRad(lat2));
        const x = Math.cos(toRad(lat1)) * Math.sin(toRad(lat2)) - Math.sin(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.cos(dLon);
        return (Math.atan2(y, x) * 180) / Math.PI; // 0 = north, clockwise
    }

    private carIcon(color: string): L.DivIcon {
        const svg = `
            <svg width="26" height="26" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <rect x="6.5" y="2" width="11" height="20" rx="3.5" fill="${color}" stroke="rgba(0,0,0,.45)" stroke-width="0.6"/>
              <rect x="8" y="3.6" width="8" height="4" rx="1.2" fill="rgba(255,255,255,.85)"/>
              <rect x="8" y="14" width="8" height="4.6" rx="1.2" fill="rgba(255,255,255,.55)"/>
              <rect x="5" y="6.5" width="1.6" height="3" rx="0.6" fill="#1f2937"/>
              <rect x="17.4" y="6.5" width="1.6" height="3" rx="0.6" fill="#1f2937"/>
            </svg>`;
        return L.divIcon({
            html: `<div class="fleet-car" style="width:26px;height:26px;transform:rotate(0deg);transition:transform .4s linear;filter:drop-shadow(0 1px 2px rgba(0,0,0,.4))">${svg}</div>`,
            className: 'fleet-car-icon',
            iconSize: [26, 26],
            iconAnchor: [13, 13],
            tooltipAnchor: [0, -14]
        });
    }

    focus(s: ShipmentSummary) {
        this.selectedId = s.id;
        this.routeLayers.forEach((line, id) => line.setStyle({ opacity: id === s.id ? .95 : .25, weight: id === s.id ? 5 : 3 }));
        if (s.currentLatitude != null && s.currentLongitude != null && this.map) {
            this.map.setView([s.currentLatitude, s.currentLongitude], 9);
        }
    }

    newShipment() {
        this.creating = true;
        this.api.create(crypto.randomUUID(), crypto.randomUUID()).subscribe({
            next: (r) =>
                this.api.dispatch(r.id).subscribe({
                    next: () => {
                        this.creating = false;
                        this.toast.add({ severity: 'success', summary: 'OK', detail: 'Envío creado y despachado' });
                        this.load();
                    },
                    error: () => {
                        this.creating = false;
                        this.load();
                    }
                }),
            error: () => {
                this.creating = false;
                this.toast.add({ severity: 'error', summary: 'Error', detail: 'No se pudo crear el envío' });
            }
        });
    }

    severity(status: string): Sev {
        switch (status) {
            case 'InTransit': return 'info';
            case 'Delivered': return 'success';
            case 'Dispatched': return 'warn';
            default: return 'secondary';
        }
    }

    ngOnDestroy() {
        this.destroyed = true;
        this.routeAbort.abort();
        this.mapResize?.disconnect();
        this.connection?.stop();
        this.map?.remove();
    }
}
