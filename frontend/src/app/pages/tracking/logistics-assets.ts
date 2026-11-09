import * as THREE from 'three';
import { RoundedBoxGeometry } from 'three/addons/geometries/RoundedBoxGeometry.js';

type Vec3 = [number, number, number];
type Part = { position: Vec3; size?: Vec3; rotation?: Vec3 };
interface Finish { roughness?: number; metalness?: number; emissive?: string; glow?: number; flat?: boolean }

/** Resources belong to one model; removing a truck cannot invalidate another. */
class Model {
    readonly group = new THREE.Group();
    private readonly geometries = new Map<string, THREE.BufferGeometry>();
    private readonly materials = new Map<string, THREE.MeshStandardMaterial>();

    /** `glow` above 1 pushes the surface over the bloom threshold, so lamps and lit windows shine. */
    material(color: string, { roughness = .65, metalness = .15, emissive, glow = 1, flat = false }: Finish = {}) {
        const key = [color, roughness, metalness, emissive ?? '', glow, flat].join('/');
        let material = this.materials.get(key);
        if (!material) {
            material = new THREE.MeshStandardMaterial({ color, roughness, metalness, emissive: emissive ?? '#000000', emissiveIntensity: glow, flatShading: flat });
            this.materials.set(key, material);
        }
        return material;
    }

    geometry<T extends THREE.BufferGeometry>(key: string, create: () => T): T {
        let geometry = this.geometries.get(key);
        if (!geometry) { geometry = create(); this.geometries.set(key, geometry); }
        return geometry as T;
    }

    add(name: string, geometry: THREE.BufferGeometry, material: THREE.Material, position: Vec3 = [0, 0, 0], rotation: Vec3 = [0, 0, 0], castShadow = true) {
        const mesh = new THREE.Mesh(geometry, material);
        mesh.name = name;
        mesh.position.set(...position);
        mesh.rotation.set(...rotation);
        mesh.castShadow = castShadow;
        mesh.receiveShadow = true;
        this.group.add(mesh);
        return mesh;
    }

    box(name: string, size: Vec3, position: Vec3, material: THREE.Material, castShadow = true, rotation: Vec3 = [0, 0, 0]) {
        const mesh = this.add(name, this.geometry('box', () => new THREE.BoxGeometry(1, 1, 1)), material, position, rotation, castShadow);
        mesh.scale.set(...size);
        return mesh;
    }

    /** Box with softened edges, which catch highlights instead of reading as a flat block. */
    rounded(name: string, size: Vec3, radius: number, position: Vec3, material: THREE.Material, segments = 3) {
        return this.add(name, this.geometry(['rounded', ...size, radius, segments].join('/'), () => new RoundedBoxGeometry(size[0], size[1], size[2], segments, radius)), material, position);
    }

    instances(name: string, geometry: THREE.BufferGeometry, material: THREE.Material, parts: Part[], castShadow = true) {
        const mesh = new THREE.InstancedMesh(geometry, material, parts.length);
        mesh.name = name;
        mesh.castShadow = castShadow;
        mesh.receiveShadow = true;
        const transform = new THREE.Object3D();
        parts.forEach((part, i) => {
            transform.position.set(...part.position);
            transform.scale.set(...(part.size ?? [1, 1, 1]));
            transform.rotation.set(...(part.rotation ?? [0, 0, 0]));
            transform.updateMatrix();
            mesh.setMatrixAt(i, transform.matrix);
        });
        mesh.instanceMatrix.needsUpdate = true;
        mesh.computeBoundingBox();
        mesh.computeBoundingSphere();
        this.group.add(mesh);
        return mesh;
    }

    boxes(name: string, material: THREE.Material, parts: Part[], castShadow = true) {
        return this.instances(name, this.geometry('box', () => new THREE.BoxGeometry(1, 1, 1)), material, parts, castShadow);
    }
}

/** Side profile drawn in (z, y) and extruded across the vehicle/building width, centred on x. */
function profile(points: (shape: THREE.Shape) => void, width: number, bevel: number, smooth: number): THREE.BufferGeometry {
    const shape = new THREE.Shape();
    points(shape);
    const geometry = new THREE.ExtrudeGeometry(shape, { depth: width - bevel * 2, bevelEnabled: bevel > 0, bevelThickness: bevel, bevelSize: bevel, bevelSegments: smooth, curveSegments: smooth * 3 });
    geometry.rotateY(-Math.PI / 2);
    geometry.translate(width / 2 - bevel, 0, 0);
    return geometry;
}

/** Slab with rounded corners lying on the ground: y in [0, height]. */
export function slab(width: number, depth: number, height: number, radius: number): THREE.BufferGeometry {
    const w = width / 2, d = depth / 2, shape = new THREE.Shape();
    shape.moveTo(-w + radius, -d);
    shape.lineTo(w - radius, -d); shape.quadraticCurveTo(w, -d, w, -d + radius);
    shape.lineTo(w, d - radius); shape.quadraticCurveTo(w, d, w - radius, d);
    shape.lineTo(-w + radius, d); shape.quadraticCurveTo(-w, d, -w, d - radius);
    shape.lineTo(-w, -d + radius); shape.quadraticCurveTo(-w, -d, -w + radius, -d);
    const geometry = new THREE.ExtrudeGeometry(shape, { depth: height, bevelEnabled: false, curveSegments: 6 });
    geometry.rotateX(-Math.PI / 2);
    return geometry;
}

let glow: THREE.CanvasTexture | undefined;
/** Soft radial falloff shared by light pools. Three re-uploads it if a model that used it is disposed. */
export function glowTexture() {
    if (!glow) {
        const canvas = document.createElement('canvas');
        canvas.width = canvas.height = 128;
        const context = canvas.getContext('2d')!;
        const gradient = context.createRadialGradient(64, 64, 0, 64, 64, 64);
        gradient.addColorStop(0, 'rgba(255,255,255,1)');
        gradient.addColorStop(.35, 'rgba(255,255,255,.42)');
        gradient.addColorStop(1, 'rgba(255,255,255,0)');
        context.fillStyle = gradient;
        context.fillRect(0, 0, 128, 128);
        glow = new THREE.CanvasTexture(canvas);
        glow.colorSpace = THREE.SRGBColorSpace;
    }
    return glow;
}

/** Light spilled on the ground by a lamp or headlight. */
function lightPool(model: Model, name: string, color: string, size: [number, number], position: Vec3, opacity: number) {
    const material = new THREE.MeshBasicMaterial({ color, map: glowTexture(), transparent: true, opacity, depthWrite: false, blending: THREE.AdditiveBlending, toneMapped: false });
    const mesh = model.add(name, model.geometry('pool', () => new THREE.PlaneGeometry(1, 1)), material, position, [-Math.PI / 2, 0, 0], false);
    mesh.receiveShadow = false;
    mesh.scale.set(size[0], size[1], 1);
    mesh.renderOrder = 1;
    return mesh;
}

/** Cab-over delivery truck with a box body. Front is +Z; tires touch y=0. No remote assets. */
export function createTruck(color = '#36cbb0', detailed = true): THREE.Group {
    const model = new Model();
    const smooth = detailed ? 3 : 1;
    const paint = model.material(color, { roughness: .3, metalness: .4 });
    const body = model.material('#dde5ea', { roughness: .5, metalness: .08 });
    const steel = model.material('#22323d', { roughness: .58, metalness: .6 });
    const glass = model.material('#0c2130', { roughness: .06, metalness: .92 });
    const rubber = model.material('#10171c', { roughness: .93, metalness: 0 });
    const chrome = model.material('#cbd8df', { roughness: .2, metalness: .95 });
    model.group.name = detailed ? 'Delivery truck' : 'Fleet truck';

    model.box('Chassis rails', [.80, .13, 2.80], [0, .41, -.03], steel);
    // Cab: vertical nose, raked windshield and a rounded roof line.
    model.add('Cab', profile(shape => {
        shape.moveTo(.60, .46); shape.lineTo(1.34, .46); shape.quadraticCurveTo(1.44, .46, 1.44, .56);
        shape.lineTo(1.44, .98); shape.lineTo(1.27, 1.48); shape.quadraticCurveTo(1.24, 1.56, 1.14, 1.56);
        shape.lineTo(.60, 1.56); shape.closePath();
    }, 1.20, .04, smooth), paint);
    model.box('Windshield', [1.04, .47, .02], [0, 1.262, 1.403], glass, false, [.33, 0, 0]);
    model.boxes('Side glass', glass, [-1, 1].map(side => ({ position: [side * .603, 1.24, .97], size: [.016, .38, .50] })), false);
    model.boxes('Wheel arches', steel, [-1, 1].map(side => ({ position: [side * .603, .52, 1.00], size: [.02, .20, .68] })), false);
    // Roof fairing bridges the cab and the taller cargo body.
    model.add('Roof fairing', profile(shape => {
        shape.moveTo(.58, 1.58); shape.lineTo(1.10, 1.58); shape.quadraticCurveTo(.86, 1.86, .58, 1.90); shape.closePath();
    }, 1.08, .03, smooth), paint);
    model.rounded('Cargo body', [1.26, 1.32, 2.02], .05, [0, 1.18, -.46], body, smooth);
    model.boxes('Cargo livery', paint, [-1, 1].flatMap(side => [
        { position: [side * .633, .70, -.46], size: [.012, .10, 1.92] }, { position: [side * .633, .82, -.46], size: [.012, .028, 1.92] }
    ] as Part[]), false);
    model.box('Front bumper', [1.22, .15, .12], [0, .47, 1.455], steel);
    model.box('Grille', [.86, .20, .02], [0, .76, 1.484], steel, false);
    model.boxes('Headlights', model.material('#fff3d6', { roughness: .2, emissive: '#ffe2a8', glow: detailed ? 5 : 2.4 }), [-1, 1].map(side => ({ position: [side * .46, .60, 1.486], size: [.20, .085, .024] })), false);
    model.boxes('Tail lights', model.material('#c2414f', { roughness: .35, emissive: '#ff3349', glow: detailed ? 5 : 2.4 }), [-1, 1].map(side => ({ position: [side * .50, .58, -1.474], size: [.16, .07, .02] })), false);
    model.box('Rear underrun bar', [1.20, .07, .07], [0, .40, -1.43], steel);
    model.boxes('Rear mudguards', steel, [-1, 1].map(side => ({ position: [side * .47, .60, -.93], size: [.36, .03, .72] })));

    const tire = model.geometry('tire', () => new THREE.LatheGeometry([[.15, -.10], [.236, -.10], [.262, -.076], [.27, -.03], [.27, .03], [.262, .076], [.236, .10], [.15, .10]].map(([r, y]) => new THREE.Vector2(r, y)), detailed ? 24 : 12));
    const wheels: Part[] = [-1, 1].flatMap(side => [
        { position: [side * .52, .27, 1.00], rotation: [0, 0, Math.PI / 2] },
        { position: [side * .47, .27, -.93], rotation: [0, 0, Math.PI / 2], size: [1, 1.65, 1] }
    ] as Part[]);
    model.instances('Tires', tire, rubber, wheels);
    model.instances('Rims', model.geometry('rim', () => new THREE.CylinderGeometry(.155, .155, .206, detailed ? 20 : 10)), chrome, wheels, false);
    model.instances('Hub caps', model.geometry('hub', () => new THREE.CylinderGeometry(.055, .055, .236, 10)), steel, wheels, false);

    if (detailed) {
        model.boxes('Grille bars', chrome, [0, 1, 2].map(i => ({ position: [0, .705 + i * .055, 1.497], size: [.80, .012, .012] })), false);
        model.box('Daytime running light', [.86, .014, .014], [0, .895, 1.488], model.material('#dffcff', { emissive: '#bdf6ff', glow: 4 }), false);
        model.boxes('Roof marker lights', model.material('#ffc66b', { emissive: '#ffb347', glow: 4.5 }), [-.36, 0, .36].map(x => ({ position: [x, 1.572, 1.17], size: [.07, .022, .035] })), false);
        model.boxes('Mirror arms', steel, [-1, 1].map(side => ({ position: [side * .66, 1.20, 1.30], size: [.13, .022, .026] })), false);
        model.boxes('Side mirrors', steel, [-1, 1].map(side => ({ position: [side * .735, 1.22, 1.30], size: [.05, .22, .09] })), false);
        model.boxes('Door seams', steel, [-1, 1].map(side => ({ position: [side * .604, .88, .70], size: [.008, .82, .012] })), false);
        model.boxes('Door handles', chrome, [-1, 1].map(side => ({ position: [side * .61, .98, .78], size: [.02, .022, .10] })), false);
        model.boxes('Cab steps', steel, [-1, 1].map(side => ({ position: [side * .59, .40, .66], size: [.10, .05, .30] })), false);
        model.boxes('Side skirts', paint, [-1, 1].map(side => ({ position: [side * .60, .44, .02], size: [.025, .17, .98] })));
        model.boxes('Mud flaps', rubber, [-1, 1].map(side => ({ position: [side * .47, .40, -1.31], size: [.32, .26, .014] })), false);
        model.box('Rear door frame', [1.16, 1.20, .018], [0, 1.18, -1.474], model.material('#d5dee4', { roughness: .5, metalness: .2 }), false);
        model.box('Rear door seam', [.014, 1.16, .02], [0, 1.18, -1.482], steel, false);
        model.boxes('Door locking bars', chrome, [-.30, -.12, .12, .30].map(x => ({ position: [x, 1.18, -1.488], size: [.016, 1.12, .016] })), false);
        const ribs: Part[] = [];
        for (const side of [-1, 1]) for (let i = 0; i < 9; i++) ribs.push({ position: [side * .632, 1.32, -1.34 + i * .22], size: [.008, .92, .014] });
        model.boxes('Cargo panel seams', model.material('#c3d0d8', { roughness: .6, metalness: .2 }), ribs, false);
        lightPool(model, 'Headlight pool', '#ffe9bf', [1.9, 2.6], [0, .03, 2.85], .34);
    }
    return model.group;
}

const CONTAINER_COLORS = [['#d27a45', '#2f7391', '#dfe6ea'], ['#3f8f7f', '#d9a441', '#34566b'], ['#b9534f', '#dfe6ea', '#2f7391']];

/**
 * Conceptual distribution centre: hall with a pitched solar roof, an office annex, loading docks,
 * a container stack, yard lighting and planting. Front is +Z. `variant` mirrors the layout and
 * changes colours so neighbouring hubs do not look identical.
 */
export function createWarehouse(color = '#36cbb0', variant = 0): THREE.Group {
    const model = new Model();
    const s = variant % 2 ? -1 : 1;      // side of the office annex
    const asphalt = model.material('#22323f', { roughness: .95, metalness: .02 });
    const kerb = model.material('#62798a', { roughness: .85, metalness: .05 });
    const cladding = model.material('#c3d0d9', { roughness: .64, metalness: .2 });
    const seam = model.material('#bccad4', { roughness: .6, metalness: .25 });
    const roof = model.material('#3a5669', { roughness: .5, metalness: .55 });
    const steel = model.material('#2a3e4d', { roughness: .55, metalness: .5 });
    const dark = model.material('#16242e', { roughness: .85, metalness: .05 });
    const accent = model.material(color, { roughness: .42, metalness: .25 });
    const glass = model.material('#0f2a3c', { roughness: .08, metalness: .9 });
    const lit = model.material('#ffe7bd', { roughness: .4, emissive: '#ffcf87', glow: 3.4 });
    const lamp = model.material('#fff6e2', { roughness: .3, emissive: '#fff0cf', glow: 6 });
    const marking = model.material('#e3c56c', { roughness: .9, metalness: 0 });
    model.group.name = 'Distribution centre';

    model.add('Kerb', slab(5.86, 4.76, .06, .42), kerb, [0, 0, .2]);
    model.add('Yard', slab(5.7, 4.6, .10, .36), asphalt, [0, 0, .2]);

    // Hall: front wall at z = .60, eaves at y = 1.70.
    model.box('Plinth', [4.04, .22, 2.34], [0, .21, -.55], steel);
    model.box('Hall', [4.0, 1.42, 2.3], [0, .99, -.55], cladding);
    model.box('Colour band', [4.03, .13, 2.33], [0, 1.50, -.55], accent);
    model.add('Pitched roof', profile(shape => { shape.moveTo(-1.27, 0); shape.lineTo(1.27, 0); shape.lineTo(1.27, .05); shape.lineTo(0, .46); shape.lineTo(-1.27, .05); shape.closePath(); }, 4.24, 0, 1), roof, [0, 1.70, -.55]);
    model.box('Ridge cap', [4.26, .04, .12], [0, 2.165, -.55], steel);
    const wallRibs: Part[] = [];
    for (const side of [-1, 1]) for (let i = 0; i < 12; i++) wallRibs.push({ position: [side * 2.004, .88, -1.60 + i * .19], size: [.012, 1.10, .022] });
    for (let i = 0; i < 21; i++) wallRibs.push({ position: [-1.9 + i * .19, .88, -1.704], size: [.022, 1.10, .012] });
    model.boxes('Cladding seams', seam, wallRibs, false);

    // Solar array on the front roof slope and skylights on the rear one.
    const pitch = Math.atan2(.41, 1.27), along = [0, -Math.sin(pitch), Math.cos(pitch)], normal = [0, Math.cos(pitch), Math.sin(pitch)];
    const onSlope = (x: number, t: number, lift: number, front: boolean): Vec3 => [x, 1.955 + along[1] * t + normal[1] * lift, -.55 + (front ? 1 : -1) * (.635 + along[2] * t + normal[2] * lift)];
    const panels: Part[] = [], frames: Part[] = [];
    for (let i = 0; i < 7; i++) for (const t of [-.28, .28]) {
        const x = -1.56 + i * .52;
        panels.push({ position: onSlope(x, t, .03, true), size: [.46, .016, .50], rotation: [pitch, 0, 0] });
        frames.push({ position: onSlope(x, t, .018, true), size: [.49, .012, .53], rotation: [pitch, 0, 0] });
    }
    model.boxes('Solar panel frames', model.material('#9fb2bf', { roughness: .35, metalness: .8 }), frames, false);
    model.boxes('Solar panels', model.material('#12305a', { roughness: .12, metalness: .75 }), panels, false);
    model.boxes('Skylights', model.material('#5d8da3', { roughness: .15, metalness: .5, emissive: '#7fd0e6', glow: .35 }), [-1.3, -.43, .43, 1.3].map(x => ({ position: onSlope(x, 0, .012, false), size: [.46, .014, .34], rotation: [-pitch, 0, 0] })), false);
    model.instances('Roof vents', model.geometry('vent', () => new THREE.CylinderGeometry(.07, .09, .16, 12)), steel, [-1.7, 0, 1.7].map(x => ({ position: [x, 2.22, -.55] })));

    // Loading docks: one bay is open and lit from inside.
    const openBay = variant % 3;
    const shutters: Part[] = [], shelters: Part[] = [], slats: Part[] = [], levellers: Part[] = [], lanes: Part[] = [], bumpers: Part[] = [], dockLights: Part[] = [];
    [1.50, .62, -.26].forEach((offset, bay) => {
        const bx = -s * offset;          // bays sit on the side opposite the annex
        shelters.push({ position: [bx, .80, .625], size: [.92, 1.06, .09] });
        if (bay === openBay) model.box('Open bay interior', [.74, .92, .02], [bx, .78, .676], lit, false);
        else {
            shutters.push({ position: [bx, .78, .676], size: [.74, .92, .02] });
            for (let i = 0; i < 7; i++) slats.push({ position: [bx, .40 + i * .125, .69], size: [.72, .012, .01] });
        }
        levellers.push({ position: [bx, .13, .86], size: [.70, .06, .36] });
        bumpers.push({ position: [bx - .30, .32, .70], size: [.07, .16, .06] }, { position: [bx + .30, .32, .70], size: [.07, .16, .06] });
        dockLights.push({ position: [bx, 1.40, .66], size: [.16, .04, .06] });
        lanes.push({ position: [bx - .44, .103, 1.62], size: [.025, .006, 1.25] }, { position: [bx + .44, .103, 1.62], size: [.025, .006, 1.25] });
    });
    model.boxes('Dock shelters', dark, shelters);
    model.boxes('Dock shutters', model.material('#aebcc6', { roughness: .55, metalness: .4 }), shutters, false);
    model.boxes('Shutter slats', model.material('#8496a2', { roughness: .6, metalness: .4 }), slats, false);
    model.boxes('Dock levellers', model.material('#8fa2ae', { roughness: .6, metalness: .6 }), levellers);
    model.boxes('Dock bumpers', dark, bumpers, false);
    model.boxes('Dock lights', lamp, dockLights, false);
    model.boxes('Lane markings', marking, lanes, false);
    model.box('Dock canopy', [2.86, .05, .42], [-s * .62, 1.47, .80], steel);

    // Office annex with a lit window grid.
    const ax = s * 1.42;
    model.rounded('Office annex', [1.16, 1.24, .92], .03, [ax, .72, 1.04], model.material('#2d4352', { roughness: .5, metalness: .35 }));
    model.box('Annex roof', [1.24, .05, 1.0], [ax, 1.365, 1.04], cladding);
    model.box('Annex roof unit', [.34, .16, .30], [ax + s * .22, 1.47, .88], seam);
    const litPanes: Part[] = [], darkPanes: Part[] = [];
    for (let row = 0; row < 2; row++) for (let col = 0; col < 3; col++) {
        const pane: Part = { position: [ax - .35 + col * .35, .55 + row * .46, 1.504], size: [.29, .32, .014] };
        if (row === 0 && col === (variant + 1) % 3) continue;      // entrance replaces one ground-floor pane
        ((row * 3 + col + variant) % 4 === 0 ? darkPanes : litPanes).push(pane);
        const side: Part = { position: [ax + s * .584, .55 + row * .46, .74 + col * .30], size: [.014, .32, .24] };
        ((row + col + variant) % 3 === 0 ? litPanes : darkPanes).push(side);
    }
    model.boxes('Lit windows', lit, litPanes, false);
    model.boxes('Dark windows', glass, darkPanes, false);
    const door = ax - .35 + ((variant + 1) % 3) * .35;
    model.box('Entrance', [.26, .46, .014], [door, .34, 1.504], glass, false);
    model.box('Entrance canopy', [.40, .03, .20], [door, .62, 1.58], accent);
    model.box('Sign', [.92, .10, .02], [ax, 1.25, 1.506], model.material(color, { roughness: .4, emissive: color, glow: 3 }), false);

    // Container stack beside the hall.
    const colors = CONTAINER_COLORS[variant % CONTAINER_COLORS.length], cx = -s * 2.42;
    const containers: [Vec3, string][] = [[[cx, .33, .78], colors[0]], [[cx, .33, -.62], colors[1]], [[cx, .79, .70], colors[2]]];
    const corrugation: Part[] = [];
    for (const [position, tone] of containers) {
        model.rounded('Container', [.50, .46, 1.26], .012, position, model.material(tone, { roughness: .55, metalness: .35 }), 1);
        for (const side of [-1, 1]) for (let i = 0; i < 10; i++) corrugation.push({ position: [position[0] + side * .252, position[1], position[2] - .54 + i * .12], size: [.008, .40, .03] });
    }
    model.boxes('Container corrugation', model.material('#1b2a34', { roughness: .7, metalness: .3 }), corrugation, false);

    // Yard lighting.
    const poles: Part[] = [], arms: Part[] = [], heads: Part[] = [];
    for (const side of [-1, 1]) {
        poles.push({ position: [side * 2.62, 1.12, 2.22] });
        arms.push({ position: [side * 2.44, 2.13, 2.22], size: [.40, .025, .035] });
        heads.push({ position: [side * 2.28, 2.105, 2.22], size: [.20, .03, .09] });
        lightPool(model, 'Yard light pool', '#ffe2ad', [2.6, 2.6], [side * 2.2, .106, 2.0], .2);
    }
    model.instances('Light poles', model.geometry('pole', () => new THREE.CylinderGeometry(.022, .03, 2.04, 8)), steel, poles);
    model.boxes('Light arms', steel, arms, false);
    model.boxes('Lamp heads', lamp, heads, false);
    lightPool(model, 'Dock light pool', '#ffdca0', [3.4, 1.9], [-s * .62, .107, 1.35], .16);

    // Pallets waiting at the dock and planting around the plot.
    const px = -s * 1.95;
    model.boxes('Pallets', model.material('#a88a5c', { roughness: .95 }), [{ position: [px, .125, 1.95], size: [.34, .05, .30] }, { position: [px + s * .42, .125, 2.02], size: [.34, .05, .30] }]);
    model.boxes('Cartons', model.material('#c8a877', { roughness: .9 }), [
        { position: [px, .25, 1.95], size: [.28, .20, .26] }, { position: [px, .42, 1.93], size: [.20, .14, .20] }, { position: [px + s * .42, .24, 2.02], size: [.30, .18, .24] }
    ]);
    const trees: Vec3[] = [[s * 2.45, 0, -1.45], [s * 2.42, 0, -.42], [-s * 2.5, 0, -1.72], [s * 2.5, 0, .28]];
    model.instances('Tree trunks', model.geometry('trunk', () => new THREE.CylinderGeometry(.03, .045, .42, 6)), model.material('#5a4636', { roughness: .95 }), trees.map(([x, , z]) => ({ position: [x, .30, z] })));
    const canopy: Part[] = [];
    trees.forEach(([x, , z], i) => {
        const scale = .9 + (i + variant) % 3 * .13;
        canopy.push({ position: [x, .72 * scale, z], size: [.30 * scale, .34 * scale, .30 * scale], rotation: [0, i * 1.3, 0] }, { position: [x + .04, 1.02 * scale, z - .02], size: [.20 * scale, .24 * scale, .20 * scale], rotation: [0, i * 2.1, .2] });
    });
    model.instances('Tree canopies', model.geometry('canopy', () => new THREE.IcosahedronGeometry(1, 1)), model.material('#3f8a70', { roughness: .9, flat: true }), canopy);
    return model.group;
}

/** Dispose each owned GPU resource once, including CanvasTextures used by labels. */
export function disposeObject(object: THREE.Object3D) {
    const instances = new Set<THREE.InstancedMesh>();
    const geometries = new Set<THREE.BufferGeometry>();
    const materials = new Set<THREE.Material>();
    const textures = new Set<THREE.Texture>();
    object.traverse(child => {
        const drawable = child as THREE.Mesh;
        if ((drawable as THREE.InstancedMesh).isInstancedMesh) instances.add(drawable as THREE.InstancedMesh);
        if (drawable.geometry) geometries.add(drawable.geometry);
        if (drawable.material) for (const material of Array.isArray(drawable.material) ? drawable.material : [drawable.material]) materials.add(material);
    });
    for (const instance of instances) instance.dispose();
    for (const material of materials) {
        for (const value of Object.values(material)) if ((value as THREE.Texture | null)?.isTexture) textures.add(value as THREE.Texture);
    }
    for (const texture of textures) texture.dispose();
    for (const material of materials) material.dispose();
    for (const geometry of geometries) geometry.dispose();
}
