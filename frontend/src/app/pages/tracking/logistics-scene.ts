import { AfterViewInit, Component, ElementRef, EventEmitter, Input, NgZone, OnChanges, OnDestroy, Output, ViewChild, inject } from '@angular/core';
import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { ShipmentSummary } from '../../core/models';
import { createTruck, createWarehouse, disposeObject } from './logistics-assets';

export type RouteCoordinates = Record<string, [number, number][]>;

@Component({
    selector: 'app-logistics-scene',
    standalone: true,
    template: `<div #host class="scene-host" role="img" aria-label="Vista 3D esquemática de envíos. Arrastra para rotar y usa la rueda para ampliar."></div>
        @if (failed) { <div class="fallback">La vista 3D no está disponible en este dispositivo. Usa el mapa de calles.</div> }
        <div class="scene-note">{{ shipments.length ? 'Coordenadas reales · escala esquemática' : 'Vista de muestra · sin telemetría' }}<br/><span>Arrastra para rotar · rueda para ampliar</span></div>
        <button class="reset" type="button" (click)="reset()" aria-label="Restablecer cámara 3D"><i aria-hidden="true" class="pi pi-compass"></i></button>`,
    styles: [`:host{display:block;position:relative;height:100%;min-height:480px;background:#0b1b29}.scene-host{position:absolute;inset:0}.scene-note{position:absolute;bottom:22px;left:24px;color:#b8cddc;font-size:11px;line-height:1.9;pointer-events:none}.scene-note span{color:#809bae}.reset{position:absolute;bottom:26px;right:22px;border:1px solid #405b70;background:#163044;color:#ddf6ef;border-radius:10px;width:38px;height:38px;cursor:pointer}.fallback{position:absolute;inset:35% 15%;text-align:center;color:#d4e5ef}`]
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
    private camera = new THREE.PerspectiveCamera(40, 1, .1, 300);
    private controls?: OrbitControls;
    private content = new THREE.Group();
    private observer?: ResizeObserver;
    private destroyed = false;
    private pointerStart = new THREE.Vector2();
    failed = false;

    ngAfterViewInit() {
        try {
            this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: false });
            this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.75));
            this.renderer.setClearColor('#0b1b29');
            this.renderer.shadowMap.enabled = true;
            this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
            this.host.nativeElement.appendChild(this.renderer.domElement);
            this.scene.add(new THREE.HemisphereLight('#d8f2ff', '#233d4d', 2.5));
            const light = new THREE.DirectionalLight('#ffffff', 3);
            light.position.set(12, 30, 10);
            light.castShadow = true;
            light.shadow.mapSize.set(1024, 1024);
            light.shadow.camera.left = light.shadow.camera.bottom = -35;
            light.shadow.camera.right = light.shadow.camera.top = 35;
            this.scene.add(light);
            const ground = new THREE.Mesh(new THREE.BoxGeometry(64, .45, 48), new THREE.MeshStandardMaterial({ color: '#102b3b', roughness: .9 }));
            ground.position.y = -.4;
            ground.receiveShadow = true;
            this.scene.add(ground);
            const grid = new THREE.GridHelper(64, 32, '#2b5367', '#193e51');
            grid.position.y = -.15;
            this.scene.add(grid, this.content);
            this.controls = new OrbitControls(this.camera, this.renderer.domElement);
            this.controls.enableDamping = true;
            this.controls.minDistance = 12;
            this.controls.maxDistance = 100;
            this.controls.maxPolarAngle = Math.PI / 2.15;
            this.reset();
            this.rebuild();
            this.observer = new ResizeObserver(() => this.resize());
            this.observer.observe(this.host.nativeElement);
            this.renderer.domElement.addEventListener('pointerdown', this.pointerDown);
            this.renderer.domElement.addEventListener('pointerup', this.pick);
            this.zone.runOutsideAngular(() => this.renderer!.setAnimationLoop(() => {
                if (this.destroyed || document.hidden || !this.host.nativeElement.clientWidth) return;
                this.controls?.update();
                this.renderer!.render(this.scene, this.camera);
            }));
        } catch {
            this.failed = true;
            this.renderer?.dispose();
        }
    }

    ngOnChanges() { if (this.renderer && !this.failed) this.rebuild(); }

    reset() { this.camera.position.set(38, 40, 48).multiplyScalar(Math.max(1, .95 / this.camera.aspect)); this.controls?.target.set(0, 0, 0); this.controls?.update(); }

    private resize() {
        const { clientWidth: width, clientHeight: height } = this.host.nativeElement;
        if (!width || !height || !this.renderer) return;
        const changedAspect = Math.abs(this.camera.aspect - width / height) > .01;
        this.camera.aspect = width / height;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(width, height);
        if (changedAspect) this.reset();
    }

    private rebuild() {
        disposeObject(this.content);
        this.content.clear();
        const points: [number, number][] = [];
        for (const s of this.shipments) {
            for (const p of [[s.currentLatitude, s.currentLongitude], [s.originLatitude, s.originLongitude], [s.destinationLatitude, s.destinationLongitude]]) {
                if (this.valid(p)) points.push(p as [number, number]);
            }
            points.push(...(this.routes[s.id] ?? []));
        }
        const bounds = points.reduce((b, p) => ({ minLat: Math.min(b.minLat, p[0]), maxLat: Math.max(b.maxLat, p[0]), minLng: Math.min(b.minLng, p[1]), maxLng: Math.max(b.maxLng, p[1]) }), { minLat: Infinity, maxLat: -Infinity, minLng: Infinity, maxLng: -Infinity });
        const midLat = points.length ? (bounds.minLat + bounds.maxLat) / 2 : 0;
        const midLng = points.length ? (bounds.minLng + bounds.maxLng) / 2 : 0;
        const cos = Math.max(.1, Math.cos(midLat * Math.PI / 180));
        const span = points.length ? Math.max(bounds.maxLat - bounds.minLat, (bounds.maxLng - bounds.minLng) * cos, .02) : 1;
        const project = (lat: number, lng: number, y = .05) => new THREE.Vector3((lng - midLng) * cos / span * 36, y, -(lat - midLat) / span * 36);
        if (!this.shipments.length) {
            const hub = createWarehouse(); hub.position.set(-9, 0, -5); this.content.add(hub);
            const truck = createTruck(); truck.position.set(2, 0, 3); truck.rotation.y = .7; this.content.add(truck);
            const other = createWarehouse('#6c9bdf'); other.position.set(11, 0, 8); this.content.add(other);
            this.line([new THREE.Vector3(-9, .05, -3), new THREE.Vector3(-9, .05, 3), new THREE.Vector3(11, .05, 3), new THREE.Vector3(11, .05, 7)], '#36cbb0', true);
        }
        const hubs = new Set<string>();
        for (const s of this.shipments) {
            const color = s.id === this.selectedId ? '#64f6cf' : '#5f94ba';
            const coords = this.routes[s.id];
            if (coords?.length) this.line(coords.map(p => project(p[0], p[1])), color, false);
            else if (this.valid([s.originLatitude, s.originLongitude]) && this.valid([s.destinationLatitude, s.destinationLongitude])) {
                this.line([project(s.originLatitude!, s.originLongitude!), project(s.destinationLatitude!, s.destinationLongitude!)], color, true);
            }
            for (const [lat, lng] of [[s.originLatitude, s.originLongitude], [s.destinationLatitude, s.destinationLongitude]]) {
                const key = `${lat},${lng}`;
                if (this.valid([lat, lng]) && !hubs.has(key)) { hubs.add(key); const hub = createWarehouse(color); hub.scale.setScalar(.65); hub.position.copy(project(lat!, lng!)); this.content.add(hub); }
            }
            if (this.valid([s.currentLatitude, s.currentLongitude])) {
                const truck = createTruck(color);
                truck.position.copy(project(s.currentLatitude!, s.currentLongitude!));
                truck.userData['shipmentId'] = s.id;
                this.content.add(truck);
                if (s.id === this.selectedId) {
                    const ring = new THREE.Mesh(new THREE.RingGeometry(1.2, 1.4, 48), new THREE.MeshBasicMaterial({ color, side: THREE.DoubleSide }));
                    ring.rotation.x = -Math.PI / 2; ring.position.copy(truck.position); ring.position.y = .12; this.content.add(ring);
                }
            }
        }
    }

    private valid(p: (number | null | undefined)[]): boolean { return p.length === 2 && p.every(v => v != null && Number.isFinite(v)) && Math.abs(p[0]!) <= 90 && Math.abs(p[1]!) <= 180; }

    private line(points: THREE.Vector3[], color: string, dashed: boolean) {
        // Raise routes above the receiving surface to avoid depth flicker.
        points.forEach(point => point.y = .15);
        const material = dashed ? new THREE.LineDashedMaterial({ color, dashSize: .5, gapSize: .3 }) : new THREE.LineBasicMaterial({ color });
        const line = new THREE.Line(new THREE.BufferGeometry().setFromPoints(points), material);
        line.computeLineDistances(); this.content.add(line);
    }

    private pointerDown = (event: PointerEvent) => this.pointerStart.set(event.clientX, event.clientY);
    private pick = (event: PointerEvent) => {
        if (this.pointerStart.distanceTo(new THREE.Vector2(event.clientX, event.clientY)) > 5) return;
        const rect = this.renderer!.domElement.getBoundingClientRect();
        const ray = new THREE.Raycaster();
        ray.setFromCamera(new THREE.Vector2((event.clientX - rect.left) / rect.width * 2 - 1, -(event.clientY - rect.top) / rect.height * 2 + 1), this.camera);
        for (const hit of ray.intersectObjects(this.content.children, true)) {
            let object: THREE.Object3D | null = hit.object;
            while (object && !object.userData['shipmentId']) object = object.parent;
            if (object) { this.zone.run(() => this.selected.emit(object!.userData['shipmentId'])); break; }
        }
    };

    ngOnDestroy() {
        this.destroyed = true;
        this.observer?.disconnect();
        this.renderer?.setAnimationLoop(null);
        this.renderer?.domElement.removeEventListener('pointerdown', this.pointerDown);
        this.renderer?.domElement.removeEventListener('pointerup', this.pick);
        this.controls?.dispose();
        disposeObject(this.scene);
        this.renderer?.dispose();
    }
}
