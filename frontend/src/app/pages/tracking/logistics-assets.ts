import * as THREE from 'three';

function box(parent: THREE.Group, size: number[], position: number[], color: string) {
    const mesh = new THREE.Mesh(new THREE.BoxGeometry(size[0], size[1], size[2]), new THREE.MeshStandardMaterial({ color, roughness: .65, metalness: .15 }));
    mesh.position.set(position[0], position[1], position[2]);
    mesh.castShadow = true;
    mesh.receiveShadow = true;
    parent.add(mesh);
    return mesh;
}

/** Local, reusable assets: no remote model downloads or embedded credentials. Front faces +Z. */
export function createTruck(color = '#36cbb0'): THREE.Group {
    const truck = new THREE.Group();
    box(truck, [1.2, .18, 2.8], [0, .42, 0], '#24394b');
    box(truck, [1.16, 1.05, 1.8], [0, 1.03, -.4], '#e6eef4');
    box(truck, [1.2, .85, .8], [0, .9, .98], color);
    box(truck, [1.05, .32, .03], [0, 1.1, 1.4], '#193447');
    box(truck, [.07, .3, .5], [-.61, 1.1, 1], '#193447');
    box(truck, [.07, .3, .5], [.61, 1.1, 1], '#193447');
    box(truck, [1.19, .12, 1.8], [0, 1.1, -.4], color);
    for (const x of [-.43, .43]) box(truck, [.18, .12, .04], [x, .66, 1.41], '#fff4c6');
    for (const x of [-.64, .64]) for (const z of [-.95, -.5, .96]) {
        const wheel = new THREE.Mesh(new THREE.CylinderGeometry(.25, .25, .18, 12), new THREE.MeshStandardMaterial({ color: '#13202b' }));
        wheel.rotation.z = Math.PI / 2;
        wheel.position.set(x, .28, z);
        truck.add(wheel);
    }
    return truck;
}

export function createWarehouse(color = '#36cbb0'): THREE.Group {
    const hub = new THREE.Group();
    box(hub, [3.8, .18, 3], [0, .1, 0], '#29475a');
    box(hub, [3, 1.3, 2], [0, .8, -.25], '#b3c8d7');
    box(hub, [3.2, .18, 2.2], [0, 1.55, -.25], '#e4edf2');
    box(hub, [3, .15, .05], [0, 1.22, .78], color);
    for (const x of [-.95, 0, .95]) {
        box(hub, [.6, .8, .06], [x, .6, .79], '#203849');
        box(hub, [.65, .07, .5], [x, .2, 1], '#7693a8');
    }
    box(hub, [.12, 2.5, .12], [1.8, 1.3, .9], '#a8c3d3');
    box(hub, [.65, .38, .06], [1.5, 2.3, .9], color);
    return hub;
}

export function disposeObject(object: THREE.Object3D) {
    object.traverse((child) => {
        const mesh = child as THREE.Mesh;
        mesh.geometry?.dispose();
        if (mesh.material) for (const material of Array.isArray(mesh.material) ? mesh.material : [mesh.material]) material.dispose();
    });
}
