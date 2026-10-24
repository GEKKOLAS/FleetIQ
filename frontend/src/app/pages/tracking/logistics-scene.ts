import { AfterViewInit, Component, ElementRef, EventEmitter, Input, NgZone, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild, inject } from '@angular/core';
import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { ShipmentSummary } from '../../core/models';
import { createTruck, createWarehouse, disposeObject } from './logistics-assets';
import { createNetworkStage, createPlaceLabel, createRouteVisual, RouteVisual } from './logistics-visuals';

export type RouteCoordinates = Record<string, [number, number][]>;

interface VehicleView {
    root: THREE.Group;
    model: THREE.Group;
    target: THREE.Vector3;
    heading: number;
    selected: boolean;
    status: string;
    label?: THREE.Sprite;
    ring?: THREE.Mesh<THREE.RingGeometry, THREE.MeshBasicMaterial>;
}

@Component({
    selector: 'app-logistics-scene',
    standalone: true,
    template: `<div #host class="scene-host" role="img" aria-label="Red logística 3D con almacenes, rutas y vehículos. Arrastra para rotar y usa la rueda para ampliar."></div>
        @if (failed) { <div class="fallback">La vista 3D no está disponible en este dispositivo. Usa el mapa de calles.</div> }
        @if (!failed && shipments.length && !hasCoordinates) { <div class="fallback">Estos envíos todavía no tienen coordenadas disponibles.</div> }
        <div class="scene-tools" aria-label="Controles de la escena">
            <button type="button" (click)="focusSelected()" [disabled]="!selectedHasPosition" title="Acercar al envío seleccionado" aria-label="Acercar al envío seleccionado"><i aria-hidden="true" class="pi pi-bullseye"></i><span>Ver envío</span></button>
            <button type="button" (click)="toggleLabels()" [attr.aria-pressed]="labelsVisible" title="Mostrar nombres de ubicaciones" aria-label="Mostrar nombres de ubicaciones"><i aria-hidden="true" class="pi pi-tag"></i><span>Etiquetas</span></button>
        </div>
        <div class="scene-note"><span class="scene-note-title">{{ shipments.length ? 'RED DE DISTRIBUCIÓN' : 'EXPLORA LA RED' }}</span><br/>
            {{ shipments.length ? (hasVehiclePositions ? 'Últimas posiciones GPS · escala esquemática' : 'Ubicaciones registradas · sin posición GPS') : 'Escena de muestra · sin telemetría' }}<br/>
            <span>Arrastra para rotar · rueda o pellizco para ampliar</span></div>
        <button class="reset" type="button" (click)="reset()" title="Ver toda la red" aria-label="Restablecer cámara y ver toda la red"><i aria-hidden="true" class="pi pi-compass"></i></button>`,
    styles: [`:host{display:block;position:relative;height:100%;min-height:480px;background:#0b1b29}.scene-host{position:absolute;inset:0}.scene-host canvas{display:block;touch-action:none;cursor:grab}.scene-host canvas:active{cursor:grabbing}.scene-tools{position:absolute;top:18px;right:18px;display:flex;gap:6px}.scene-tools button,.reset{border:1px solid #3b5b6c;background:#112d3eea;color:#c4e2e6;border-radius:9px;cursor:pointer;backdrop-filter:blur(8px)}.scene-tools button{display:flex;align-items:center;gap:7px;padding:9px 11px;font-size:10px}.scene-tools button[aria-pressed=true]{color:#93f0d3;border-color:#3b807c}.scene-tools button:disabled{opacity:.45;cursor:default}.scene-tools button:focus-visible,.reset:focus-visible{outline:2px solid #69e7c2;outline-offset:3px}.scene-note{position:absolute;bottom:20px;left:22px;color:#b1cbd7;font-size:10px;line-height:1.9;pointer-events:none;text-shadow:0 1px 6px #081b28}.scene-note-title{color:#8fd9cc!important;font-size:9px;font-weight:700;letter-spacing:1.7px}.scene-note span{color:#7d9bab}.reset{position:absolute;bottom:24px;right:20px;width:38px;height:38px}.fallback{position:absolute;inset:35% 15%;text-align:center;color:#d4e5ef;font-size:13px;pointer-events:none}@media(max-width:520px){.scene-tools{top:57px;right:12px}.scene-tools button{padding:8px}.scene-tools button span{display:none}.scene-note{left:14px;bottom:16px;font-size:9px;max-width:calc(100% - 67px)}.reset{right:14px;bottom:20px}}`]
})
export class LogisticsScene implements AfterViewInit, OnChanges, OnDestroy {
    @Input() shipments: ShipmentSummary[] = [];
    @Input() routes: RouteCoordinates = {};
    @Input() selectedId?: string;
    @Output() selected = new EventEmitter<string>();
    @ViewChild('host') host!: ElementRef<HTMLDivElement>;
    private zone = inject(NgZone);
    private renderer?: THREE.WebGLRenderer;
    private scene = new THREE.Scene();
    private camera = new THREE.PerspectiveCamera(40, 1, .1, 400);
    private controls?: OrbitControls;
    private network = new THREE.Group();
    private labels = new THREE.Group();
    private stage = new THREE.Group();
    private vehicleLayer = new THREE.Group();
    private vehicles = new Map<string, VehicleView>();
    private roadViews = new Map<string, RouteVisual>();
    private observer?: ResizeObserver;
    private keyLight?: THREE.DirectionalLight;
    private destroyed = false;
    private pointerStart = new THREE.Vector2();
    private topology = '';
    private frameBounds = new THREE.Box3();
    private hasFitted = false;
    private lastFrame = 0;
    private lastLabelLayout = -1000;
    private reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    private project = (lat: number, lng: number) => new THREE.Vector3(lng, 0, -lat);
    failed = false;
    labelsVisible = true;

    get hasCoordinates() { return this.shipments.some(s => this.valid([s.currentLatitude, s.currentLongitude]) || this.valid([s.originLatitude, s.originLongitude]) || this.valid([s.destinationLatitude, s.destinationLongitude])); }
    get hasVehiclePositions() { return this.shipments.some(s => this.valid([s.currentLatitude, s.currentLongitude])); }
    get selectedHasPosition() { const s = this.shipments.find(item => item.id === this.selectedId); return !!s && this.valid([s.currentLatitude, s.currentLongitude]); }

    ngAfterViewInit() {
        try {
            this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: false, powerPreference: 'high-performance' });
            this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.65));
            this.renderer.setClearColor('#0b1b29');
            this.renderer.outputColorSpace = THREE.SRGBColorSpace;
            this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
            this.renderer.toneMappingExposure = 1.12;
            this.renderer.shadowMap.enabled = true;
            this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
            this.host.nativeElement.appendChild(this.renderer.domElement);
            this.scene.fog = new THREE.Fog('#0b1b29', 100, 220);
            this.scene.add(new THREE.HemisphereLight('#d8efff', '#213e4b', 2.2));
            this.keyLight = new THREE.DirectionalLight('#fff2dc', 3.1);
            this.keyLight.position.set(-18, 30, 18);
            this.keyLight.castShadow = true;
            this.keyLight.shadow.mapSize.set(1024, 1024);
            this.keyLight.shadow.camera.left = this.keyLight.shadow.camera.bottom = -35;
            this.keyLight.shadow.camera.right = this.keyLight.shadow.camera.top = 35;
            this.keyLight.shadow.normalBias = .04;
            const fill = new THREE.DirectionalLight('#8cc8ee', 1.1);
            fill.position.set(20, 14, -16);
            this.scene.add(this.keyLight, fill, this.network, this.vehicleLayer);
            this.resize();
            this.controls = new OrbitControls(this.camera, this.renderer.domElement);
            this.controls.enableDamping = true;
            this.controls.dampingFactor = .07;
            this.controls.minDistance = 7;
            this.controls.maxDistance = 190;
            this.controls.maxPolarAngle = Math.PI / 2.18;
            this.controls.minPolarAngle = .18;
            this.refresh(true);
            this.observer = new ResizeObserver(() => this.resize());
            this.observer.observe(this.host.nativeElement);
            this.renderer.domElement.addEventListener('pointerdown', this.pointerDown);
            this.renderer.domElement.addEventListener('pointerup', this.pick);
            this.zone.runOutsideAngular(() => this.renderer!.setAnimationLoop((time: number) => {
                if (this.destroyed || document.hidden || !this.host.nativeElement.clientWidth) { this.lastFrame = time; return; }
                const dt = Math.min(.1, Math.max(0, (time - this.lastFrame) / 1000));
                this.lastFrame = time;
                const alpha = this.reducedMotion ? 1 : 1 - Math.exp(-dt * 9);
                for (const view of this.vehicles.values()) {
                    view.root.position.lerp(view.target, alpha);
                    const difference = Math.atan2(Math.sin(view.heading - view.model.rotation.y), Math.cos(view.heading - view.model.rotation.y));
                    view.model.rotation.y += difference * alpha;
                    if (view.ring) view.ring.material.opacity = this.reducedMotion ? .65 : .5 + Math.sin(time / 650) * .12;
                }
                this.controls?.update();
                if (time - this.lastLabelLayout > 70) { this.layoutLabels(); this.lastLabelLayout = time; }
                this.renderer!.render(this.scene, this.camera);
            }));
        } catch {
            this.failed = true;
            this.cleanup();
        }
    }

    ngOnChanges(changes: SimpleChanges) { if (this.renderer && !this.failed) this.refresh(!!changes['routes']); }

    reset() { this.fit(true); }

    toggleLabels() {
        this.labelsVisible = !this.labelsVisible;
        this.labels.visible = this.labelsVisible;
        this.vehicles.forEach(view => { if (view.label) view.label.visible = this.labelsVisible; });
    }

    focusSelected() {
        const vehicle = this.selectedId ? this.vehicles.get(this.selectedId) : undefined;
        if (!vehicle || !this.controls) return;
        const direction = this.camera.position.clone().sub(this.controls.target).normalize();
        const target = vehicle.root.position.clone().add(new THREE.Vector3(0, .8, 0));
        this.controls.target.copy(target);
        this.camera.position.copy(target).addScaledVector(direction, Math.max(13, 15 / Math.min(1, this.camera.aspect)));
        this.controls.update();
    }

    private resize() {
        const { clientWidth: width, clientHeight: height } = this.host.nativeElement;
        if (!width || !height || !this.renderer) return;
        const previousAspect = this.camera.aspect;
        const changed = Math.abs(previousAspect - width / height) > .01;
        this.camera.aspect = width / height;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(width, height);
        if (changed) this.fit((previousAspect < .9) !== (this.camera.aspect < .9));
        this.lastLabelLayout = -1000;
    }

    private refresh(routesChanged: boolean) {
        const key = JSON.stringify(this.shipments.map(s => [s.id, s.originLatitude, s.originLongitude, s.destinationLatitude, s.destinationLongitude, s.originCity, s.destinationCity]));
        let rebuild = routesChanged || key !== this.topology;
        if (!rebuild) {
            const safeBounds = this.frameBounds.clone().expandByScalar(2);
            rebuild = this.shipments.some(s => this.valid([s.currentLatitude, s.currentLongitude]) && !safeBounds.containsPoint(this.project(s.currentLatitude!, s.currentLongitude!)));
        }
        if (rebuild) { this.topology = key; this.rebuildNetwork(); }
        this.updateVehicles(rebuild);
        for (const [id, road] of this.roadViews) {
            const selected = id === this.selectedId;
            road.accent.color.set(selected ? '#75f2cc' : '#82b5ca');
            road.accent.opacity = selected ? 1 : .55;
            road.bed.color.set(selected ? '#367b75' : '#294b5f');
            road.group.renderOrder = selected ? 2 : 0;
            road.group.position.y = selected ? .024 : 0;
        }
        if (rebuild) this.fit(false);
    }

    private rebuildNetwork() {
        disposeObject(this.network);
        this.network.clear();
        this.labels = new THREE.Group();
        this.labels.visible = this.labelsVisible;
        this.network.add(this.labels);
        this.roadViews.clear();
        this.scene.remove(this.stage);
        disposeObject(this.stage);
        const points: [number, number][] = [];
        for (const s of this.shipments) {
            for (const p of [[s.currentLatitude, s.currentLongitude], [s.originLatitude, s.originLongitude], [s.destinationLatitude, s.destinationLongitude], ...(this.routes[s.id] ?? [])]) {
                if (this.valid(p)) points.push(p as [number, number]);
            }
        }
        const bounds = points.reduce((b, p) => ({ minLat: Math.min(b.minLat, p[0]), maxLat: Math.max(b.maxLat, p[0]), minLng: Math.min(b.minLng, p[1]), maxLng: Math.max(b.maxLng, p[1]) }), { minLat: Infinity, maxLat: -Infinity, minLng: Infinity, maxLng: -Infinity });
        const midLat = points.length ? (bounds.minLat + bounds.maxLat) / 2 : 0;
        const midLng = points.length ? (bounds.minLng + bounds.maxLng) / 2 : 0;
        const cos = Math.max(.1, Math.cos(midLat * Math.PI / 180));
        const span = points.length ? Math.max(bounds.maxLat - bounds.minLat, (bounds.maxLng - bounds.minLng) * cos, .02) : 1;
        this.project = (lat, lng) => new THREE.Vector3((lng - midLng) * cos / span * 30, 0, -(lat - midLat) / span * 30);
        this.frameBounds.makeEmpty();
        points.forEach(p => this.frameBounds.expandByPoint(this.project(...p)));
        if (!this.shipments.length) this.buildSample();
        const hubs = new Map<string, { point: THREE.Vector3; city: string; origin: boolean; destination: boolean }>();
        for (const shipment of this.shipments) {
            const coords = (this.routes[shipment.id] ?? []).filter(p => this.valid(p));
            if (coords.length > 1) this.addRoute(shipment.id, coords.map(p => this.project(...p)), false);
            else if (this.valid([shipment.originLatitude, shipment.originLongitude]) && this.valid([shipment.destinationLatitude, shipment.destinationLongitude])) {
                this.addRoute(shipment.id, [this.project(shipment.originLatitude!, shipment.originLongitude!), this.project(shipment.destinationLatitude!, shipment.destinationLongitude!)], true);
            }
            for (const end of [
                { lat: shipment.originLatitude, lng: shipment.originLongitude, city: shipment.originCity, origin: true },
                { lat: shipment.destinationLatitude, lng: shipment.destinationLongitude, city: shipment.destinationCity, origin: false }
            ]) {
                if (!this.valid([end.lat, end.lng])) continue;
                const key = `${end.lat},${end.lng}`;
                const existing = hubs.get(key);
                if (existing) { existing.origin ||= end.origin; existing.destination ||= !end.origin; }
                else hubs.set(key, { point: this.project(end.lat!, end.lng!), city: end.city || 'Ubicación registrada', origin: end.origin, destination: !end.origin });
            }
        }
        for (const hub of hubs.values()) this.addHub(hub.point, hub.city, hub.origin && hub.destination ? 'Origen / destino' : hub.origin ? 'Origen' : 'Destino', hub.origin ? '#48cbb1' : '#79aee5');
        if (this.frameBounds.isEmpty()) this.frameBounds.set(new THREE.Vector3(-10, 0, -8), new THREE.Vector3(10, 0, 8));
        this.frameBounds.expandByVector(new THREE.Vector3(4, 0, 4));
        this.frameBounds.min.y = 0;
        this.frameBounds.max.y = 5.5;
        const size = this.frameBounds.getSize(new THREE.Vector3());
        const center = this.frameBounds.getCenter(new THREE.Vector3());
        this.stage = createNetworkStage(Math.max(18, size.x + 3), Math.max(18, size.z + 3));
        this.stage.position.set(center.x, 0, center.z);
        this.scene.add(this.stage);
    }

    private buildSample() {
        this.addHub(new THREE.Vector3(-8, 0, -7), 'Centro de origen', 'Muestra', '#48cbb1');
        this.addHub(new THREE.Vector3(9, 0, 7), 'Centro de destino', 'Muestra', '#79aee5');
        this.addRoute('sample', [new THREE.Vector3(-8, 0, -7), new THREE.Vector3(-8, 0, 2), new THREE.Vector3(9, 0, 2), new THREE.Vector3(9, 0, 7)], true);
        const truck = createTruck('#48cbb1');
        truck.position.set(-1, 0, 2);
        truck.rotation.y = Math.PI / 2;
        this.network.add(truck);
        this.frameBounds.set(new THREE.Vector3(-8, 0, -7), new THREE.Vector3(9, 0, 7));
    }

    private addHub(point: THREE.Vector3, name: string, role: string, color: string) {
        const hub = createWarehouse(color);
        // Building meshes are illustrative; the node stays at its recorded coordinate.
        hub.position.copy(point).add(new THREE.Vector3(0, 0, -1.8));
        this.network.add(hub);
        const label = createPlaceLabel(name, role, color);
        label.position.copy(point).add(new THREE.Vector3(0, 4.0, -1.8));
        this.labels.add(label);
        const marker = new THREE.Mesh(new THREE.RingGeometry(.27, .37, 32), new THREE.MeshBasicMaterial({ color, transparent: true, opacity: .85, side: THREE.DoubleSide, depthWrite: false }));
        marker.rotation.x = -Math.PI / 2;
        marker.position.copy(point).y = .06;
        this.network.add(marker);
    }

    private addRoute(id: string, points: THREE.Vector3[], dashed: boolean) {
        const road = createRouteVisual(points, dashed);
        this.network.add(road.group);
        this.roadViews.set(id, road);
    }

    private updateVehicles(reproject: boolean) {
        const present = new Set<string>();
        for (const shipment of this.shipments) {
            if (!this.valid([shipment.currentLatitude, shipment.currentLongitude])) continue;
            present.add(shipment.id);
            const point = this.project(shipment.currentLatitude!, shipment.currentLongitude!);
            const selected = shipment.id === this.selectedId;
            let view = this.vehicles.get(shipment.id);
            if (!view) {
                const root = new THREE.Group();
                root.name = 'Shipment ' + shipment.id;
                root.userData['shipmentId'] = shipment.id;
                root.position.copy(point);
                const model = createTruck(selected ? '#63dfbd' : '#789cae', selected);
                root.add(model);
                this.vehicleLayer.add(root);
                const from = this.valid([shipment.originLatitude, shipment.originLongitude]) ? this.project(shipment.originLatitude!, shipment.originLongitude!) : point;
                const to = this.valid([shipment.destinationLatitude, shipment.destinationLongitude]) ? this.project(shipment.destinationLatitude!, shipment.destinationLongitude!) : point;
                const heading = from.distanceToSquared(to) > .001 ? Math.atan2(to.x - from.x, to.z - from.z) : 0;
                model.rotation.y = heading;
                view = { root, model, target: point.clone(), heading, selected, status: '' };
                this.vehicles.set(shipment.id, view);
            }
            if (view.selected !== selected) {
                view.root.remove(view.model);
                disposeObject(view.model);
                view.model = createTruck(selected ? '#63dfbd' : '#789cae', selected);
                view.model.rotation.y = view.heading;
                view.root.add(view.model);
                view.selected = selected;
                view.status = '';
            }
            if (!reproject && point.distanceToSquared(view.target) > .00001) view.heading = Math.atan2(point.x - view.target.x, point.z - view.target.z);
            view.target.copy(point);
            if (reproject) view.root.position.copy(point);
            if (selected && !view.ring) {
                view.ring = new THREE.Mesh(new THREE.RingGeometry(1.32, 1.46, 64), new THREE.MeshBasicMaterial({ color: '#6ef0c5', transparent: true, opacity: .6, side: THREE.DoubleSide, depthWrite: false, toneMapped: false }));
                view.ring.rotation.x = -Math.PI / 2;
                view.ring.position.y = .085;
                view.root.add(view.ring);
            } else if (!selected && view.ring) { view.root.remove(view.ring); disposeObject(view.ring); view.ring = undefined; }
            if ((selected && view.status !== shipment.status) || (!selected && view.label)) {
                if (view.label) { view.root.remove(view.label); disposeObject(view.label); view.label = undefined; }
                if (selected) {
                    const status = ({ InTransit: 'En tránsito', Dispatched: 'Despachado', Delivered: 'Entregado', Created: 'Creado', Cancelled: 'Cancelado' } as Record<string, string>)[shipment.status] || shipment.status;
                    view.label = createPlaceLabel('FIQ–' + shipment.id.slice(0, 8).toUpperCase(), status, '#7befcc', true);
                    view.label.position.set(0, 3.1, 0);
                    view.label.visible = this.labelsVisible;
                    view.root.add(view.label);
                }
                view.status = shipment.status;
            }
        }
        for (const [id, view] of this.vehicles) if (!present.has(id)) { this.vehicleLayer.remove(view.root); disposeObject(view.root); this.vehicles.delete(id); }
    }

    /** Fit the projected content, keeping the user's orbit during data refresh. */
    private fit(defaultAngle: boolean) {
        if (!this.controls || this.frameBounds.isEmpty()) return;
        const center = this.frameBounds.getCenter(new THREE.Vector3());
        const defaultDirection = this.camera.aspect < .9 ? new THREE.Vector3(.4, 1.3, 1.8) : new THREE.Vector3(1.1, 1.15, 1.4);
        const direction = defaultAngle || !this.hasFitted ? defaultDirection.normalize() : this.camera.position.clone().sub(this.controls.target).normalize();
        const right = new THREE.Vector3().crossVectors(new THREE.Vector3(0, 1, 0), direction).normalize();
        const up = new THREE.Vector3().crossVectors(direction, right).normalize();
        const tanVertical = Math.tan(THREE.MathUtils.degToRad(this.camera.fov / 2));
        const tanHorizontal = tanVertical * this.camera.aspect;
        let distance = 12;
        for (const x of [this.frameBounds.min.x, this.frameBounds.max.x]) for (const y of [this.frameBounds.min.y, this.frameBounds.max.y]) for (const z of [this.frameBounds.min.z, this.frameBounds.max.z]) {
            const relative = new THREE.Vector3(x, y, z).sub(center);
            distance = Math.max(distance, relative.dot(direction) + Math.max(Math.abs(relative.dot(right)) / tanHorizontal, Math.abs(relative.dot(up)) / tanVertical));
        }
        this.controls.target.copy(center);
        this.camera.position.copy(center).addScaledVector(direction, distance * 1.08);
        this.controls.update();
        this.hasFitted = true;
    }

    /** Fixed-size labels remain legible at overview; nearby labels reappear as the user zooms. */
    private layoutLabels() {
        const { clientWidth: width, clientHeight: height } = this.host.nativeElement;
        if (!width || !height) return;
        const selectedLabels = [...this.vehicles.values()].flatMap(view => view.label ? [view.label] : []);
        const labels = [...selectedLabels, ...this.labels.children as THREE.Sprite[]];
        const unit = 2 * Math.tan(THREE.MathUtils.degToRad(this.camera.fov / 2)) / height;
        const rectangles: { left: number; top: number; right: number; bottom: number }[] = [];
        this.camera.updateMatrixWorld();
        for (const label of labels) {
            const selected = !!label.userData['selectedLabel'];
            const h = selected ? 46 : 42;
            const w = h * 4;
            label.scale.set(w * unit, h * unit, 1);
            const p = label.getWorldPosition(new THREE.Vector3()).project(this.camera);
            const x = (p.x + 1) * width / 2, y = (1 - p.y) * height / 2;
            const rectangle = { left: x - w / 2 - 4, top: y - h / 2 - 4, right: x + w / 2 + 4, bottom: y + h / 2 + 4 };
            const overlaps = rectangles.some(r => rectangle.left < r.right && rectangle.right > r.left && rectangle.top < r.bottom && rectangle.bottom > r.top);
            label.visible = this.labelsVisible && p.z > -1 && p.z < 1 && rectangle.left > 5 && rectangle.right < width - 5 && rectangle.top > 63 && rectangle.bottom < height - 67 && !overlaps;
            if (label.visible) rectangles.push(rectangle);
        }
    }

    private valid(p: (number | null | undefined)[]): boolean { return p.length === 2 && p.every(v => v != null && Number.isFinite(v)) && Math.abs(p[0]!) <= 90 && Math.abs(p[1]!) <= 180; }
    private pointerDown = (event: PointerEvent) => this.pointerStart.set(event.clientX, event.clientY);
    private pick = (event: PointerEvent) => {
        if (event.button !== 0 || this.pointerStart.distanceTo(new THREE.Vector2(event.clientX, event.clientY)) > 5 || !this.renderer) return;
        const rect = this.renderer.domElement.getBoundingClientRect();
        const ray = new THREE.Raycaster();
        ray.setFromCamera(new THREE.Vector2((event.clientX - rect.left) / rect.width * 2 - 1, -(event.clientY - rect.top) / rect.height * 2 + 1), this.camera);
        for (const hit of ray.intersectObjects(this.vehicleLayer.children, true)) {
            let object: THREE.Object3D | null = hit.object;
            while (object && !object.userData['shipmentId']) object = object.parent;
            if (object) { this.zone.run(() => this.selected.emit(object!.userData['shipmentId'])); break; }
        }
    };

    private cleanup() {
        this.observer?.disconnect();
        this.renderer?.setAnimationLoop(null);
        this.renderer?.domElement.removeEventListener('pointerdown', this.pointerDown);
        this.renderer?.domElement.removeEventListener('pointerup', this.pick);
        this.controls?.dispose();
        disposeObject(this.scene);
        this.scene.clear();
        this.vehicles.clear();
        this.roadViews.clear();
        this.keyLight?.shadow.dispose();
        this.renderer?.dispose();
        this.renderer?.domElement.remove();
    }

    ngOnDestroy() { this.destroyed = true; this.cleanup(); }
}
