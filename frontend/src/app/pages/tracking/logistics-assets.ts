import * as THREE from 'three';

type Vec3 = [number, number, number];
type Part = { position: Vec3; size?: Vec3; rotation?: Vec3 };

/** Resources belong to one model; removing a truck cannot invalidate another. */
class Model {
    readonly group = new THREE.Group();
    private readonly geometries = new Map<string, THREE.BufferGeometry>();
    private readonly materials = new Map<string, THREE.MeshStandardMaterial>();

    material(color: string, roughness = .65, metalness = .15, emissive?: string) {
        const key = [color, roughness, metalness, emissive ?? ''].join('/');
        let material = this.materials.get(key);
        if (!material) {
            material = new THREE.MeshStandardMaterial({ color, roughness, metalness, emissive: emissive ?? '#000000', emissiveIntensity: .35 });
            this.materials.set(key, material);
        }
        return material;
    }

    geometry(key: string, create: () => THREE.BufferGeometry) {
        let geometry = this.geometries.get(key);
        if (!geometry) { geometry = create(); this.geometries.set(key, geometry); }
        return geometry;
    }

    box(name: string, size: Vec3, position: Vec3, material: THREE.Material, castShadow = true) {
        const mesh = new THREE.Mesh(this.geometry('box', () => new THREE.BoxGeometry(1, 1, 1)), material);
        mesh.name = name;
        mesh.scale.set(...size);
        mesh.position.set(...position);
        mesh.castShadow = castShadow;
        mesh.receiveShadow = true;
        this.group.add(mesh);
        return mesh;
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

/** Rigid delivery truck. Front is +Z; tires touch y=0. No remote assets. */
export function createTruck(color = '#36cbb0', detailed = true): THREE.Group {
    const model = new Model();
    const steel = model.material('#263d4c', .55, .5);
    const body = model.material('#e0e8ed', .58, .12);
    const cab = model.material(color, .42, .25);
    const glass = model.material('#153346', .17, .2);
    const rubber = model.material('#14232e', .96, .02);
    const chrome = model.material('#94aeba', .4, .72);
    const trim = model.material('#8da2ae', .62, .32);
    model.group.name = detailed ? 'Delivery truck' : 'Fleet truck';
    model.box('Chassis', [1.20, .16, 2.74], [0, .39, .02], steel);
    model.box('Cargo body', [1.24, 1.11, 1.92], [0, 1.065, -.36], body);
    model.box('Cargo roof cap', [1.27, .045, 1.96], [0, 1.645, -.36], body);
    model.box('Cab', [1.20, .87, .79], [0, .92, 1.015], cab);
    model.box('Cab roof', [1.24, .065, .82], [0, 1.39, 1.015], cab);
    model.box('Windshield', [1.06, .31, .024], [0, 1.16, 1.423], glass, false);
    model.box('Front bumper', [1.30, .105, .10], [0, .49, 1.43], chrome);
    model.box('Rear bumper', [1.28, .075, .08], [0, .405, -1.36], steel);
    model.boxes('Side glass', glass, [-1, 1].map(side => ({ position: [side * .612, 1.16, 1.01], size: [.024, .30, .50] })), false);
    model.boxes('Cargo stripe', cab, [-1, 1].map(side => ({ position: [side * .65, .87, -.36], size: [.018, .12, 1.87] })), false);
    model.boxes('Headlights', model.material('#fff0cd', .25, .12, '#e2bd7c'), [-1, 1].map(side => ({ position: [side * .425, .75, 1.432], size: [.20, .085, .025] })), false);
    model.boxes('Tail lights', model.material('#ba5861', .4, .12, '#742831'), [-1, 1].map(side => ({ position: [side * .46, .45, -1.406], size: [.16, .06, .018] })), false);
    const wheels: Part[] = [];
    for (const x of [-.645, .645]) for (const z of [-.99, -.51, 1.00]) wheels.push({ position: [x, .245, z], rotation: [0, 0, Math.PI / 2] });
    model.instances('Tires', model.geometry('tire', () => new THREE.CylinderGeometry(.245, .245, .15, detailed ? 20 : 12)), rubber, wheels);
    model.instances('Wheel hubs', model.geometry('hub', () => new THREE.CylinderGeometry(.115, .115, .162, detailed ? 16 : 10)), chrome, wheels, false);
    if (detailed) {
        model.box('Radiator grille', [.48, .18, .022], [0, .79, 1.435], steel, false);
        model.boxes('Grille bars', trim, [0, 1, 2].map(i => ({ position: [0, .735 + i * .055, 1.45], size: [.44, .015, .012] })), false);
        model.boxes('Mirror arms', steel, [-1, 1].map(side => ({ position: [side * .67, 1.12, 1.28], size: [.12, .025, .03] })), false);
        model.boxes('Side mirrors', steel, [-1, 1].map(side => ({ position: [side * .73, 1.17, 1.28], size: [.075, .15, .075] })), false);
        model.boxes('Door handles', chrome, [-1, 1].map(side => ({ position: [side * .619, .96, .94], size: [.025, .025, .10] })), false);
        model.boxes('Cab steps', steel, [-1, 1].map(side => ({ position: [side * .63, .46, .78], size: [.14, .065, .35] })), false);
        model.boxes('Rear cargo doors', trim, [-1, 1].map(side => ({ position: [side * .301, 1.06, -1.335], size: [.585, 1.01, .022] })), false);
        model.box('Rear door seam', [.022, 1.0, .025], [0, 1.06, -1.351], steel, false);
        model.boxes('Door locking bars', chrome, [-1, 1].map(side => ({ position: [side * .29, 1.06, -1.354], size: [.018, .89, .018] })), false);
        const ribs: Part[] = [];
        for (const side of [-1, 1]) for (let i = 0; i < 10; i++) ribs.push({ position: [side * .634, 1.09, -1.23 + i * .193], size: [.018, .99, .025] });
        model.boxes('Cargo panel ribs', model.material('#b9c9d2', .65, .2), ribs, false);
        model.box('Cab sun visor', [1.11, .045, .13], [0, 1.345, 1.43], cab);
    }
    return model.group;
}

/** Conceptual distribution endpoint with loading forecourt. Front is +Z. */
export function createWarehouse(color = '#36cbb0'): THREE.Group {
    const model = new Model();
    const concrete = model.material('#405b6d', .92, .03);
    const cladding = model.material('#b7c8d3', .68, .2);
    const pale = model.material('#dce6eb', .62, .22);
    const steel = model.material('#344e60', .58, .45);
    const dark = model.material('#1a3345', .8, .06);
    const accent = model.material(color, .5, .18);
    const stripe = model.material('#8fa7af', .9, .02);
    model.group.name = 'Distribution depot';
    model.box('Forecourt', [4.8, .10, 3.9], [0, .05, .15], concrete);
    model.box('Warehouse foundation', [4.16, .12, 2.42], [0, .16, -.48], concrete);
    model.box('Warehouse cladding', [4.08, 1.60, 2.36], [0, 1.02, -.48], cladding);
    model.box('Roof overhang', [4.28, .10, 2.56], [0, 1.87, -.48], pale);
    model.box('Roof parapet front', [4.28, .10, .07], [0, 1.97, .765], steel);
    model.box('Roof parapet rear', [4.28, .10, .07], [0, 1.97, -1.725], steel);
    model.boxes('Roof parapet sides', steel, [-1, 1].map(side => ({ position: [side * 2.105, 1.97, -.48], size: [.07, .10, 2.42] })));
    model.box('Facade fascia', [4.10, .13, .045], [0, 1.55, .722], accent, false);
    model.box('Loading canopy', [4.30, .065, .43], [0, 1.43, .865], steel);
    const ribs: Part[] = [];
    for (const side of [-1, 1]) for (let i = 0; i < 13; i++) ribs.push({ position: [side * 2.052, 1.02, -1.57 + i * .175], size: [.025, 1.50, .026] });
    for (const x of [-1.98, -.67, .67, 1.98]) ribs.push({ position: [x, 1.02, .714], size: [.025, 1.50, .035] });
    model.boxes('Corrugated wall ribs', pale, ribs, false);
    const doors: Part[] = [], frames: Part[] = [], slats: Part[] = [], aprons: Part[] = [], lines: Part[] = [];
    for (const x of [-1.35, 0, 1.35]) {
        doors.push({ position: [x, .79, .721], size: [.94, 1.10, .035] });
        frames.push({ position: [x - .50, .80, .74], size: [.055, 1.21, .075] }, { position: [x + .50, .80, .74], size: [.055, 1.21, .075] }, { position: [x, 1.39, .74], size: [1.055, .06, .075] });
        aprons.push({ position: [x, .205, .945], size: [.92, .06, .38] });
        for (let i = 0; i < 8; i++) slats.push({ position: [x, .31 + i * .135, .744], size: [.91, .014, .014] });
        lines.push({ position: [x - .48, .108, 1.57], size: [.02, .008, .86] }, { position: [x + .48, .108, 1.57], size: [.02, .008, .86] }, { position: [x, .108, 1.985], size: [.96, .008, .02] });
    }
    model.boxes('Recessed loading doors', dark, doors, false);
    model.boxes('Loading bay frames', steel, frames);
    model.boxes('Door shutter seams', model.material('#526a7b', .72, .18), slats, false);
    model.boxes('Dock levellers', stripe, aprons);
    model.boxes('Loading lane markings', stripe, lines, false);
    model.boxes('Skylight frames', steel, [-.60, .60].map(x => ({ position: [x, 1.95, -.04], size: [.77, .055, .79] })));
    model.boxes('Skylight glass', model.material('#547a90', .25, .25), [-.60, .60].map(x => ({ position: [x, 1.985, -.04], size: [.68, .025, .70] })), false);
    model.boxes('Roof ventilation units', model.material('#829aa8', .63, .45), [-1.18, 1.18].map(x => ({ position: [x, 2.04, -.99], size: [.48, .23, .52] })));
    model.instances('Ventilation fans', model.geometry('fan', () => new THREE.CylinderGeometry(.15, .15, .025, 16)), dark, [-1.18, 1.18].map(x => ({ position: [x, 2.17, -.99] })), false);
    const bollards: Part[] = [-1.93, -.81, .81, 1.93].map(x => ({ position: [x, .255, 1.16] }));
    model.instances('Dock safety bollards', model.geometry('bollard', () => new THREE.CylinderGeometry(.032, .036, .30, 10)), model.material('#bda166', .75, .15), bollards);
    model.box('Site front edge', [4.8, .035, .035], [0, .065, 2.095], accent, false);
    model.box('Pallet base', [.32, .075, .30], [-2.14, .141, 1.68], model.material('#9b825f', .94, .03));
    model.box('Illustrative parcel', [.25, .26, .23], [-2.14, .31, 1.68], model.material('#baaa89', .9, .02));
    model.box('Parcel strap', [.035, .008, .235], [-2.14, .445, 1.68], pale, false);
    model.boxes('Planters', steel, [{ position: [2.15, .22, 1.72], size: [.29, .24, .38] }, { position: [-2.15, .22, 1.04], size: [.29, .24, .38] }]);
    const foliage: Part[] = [];
    for (const x of [-2.15, 2.15]) for (let i = 0; i < 3; i++) foliage.push({ position: [x, .43, (x < 0 ? 1.04 : 1.72) - .10 + i * .10], size: [.125, .17 + i * .02, .125] });
    model.instances('Muted landscaping', model.geometry('foliage', () => new THREE.SphereGeometry(1, 10, 8)), model.material('#3d655c', .95, .02), foliage, false);
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
