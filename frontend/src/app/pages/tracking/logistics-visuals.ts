import * as THREE from 'three';
import { glowTexture, slab } from './logistics-assets';

export interface RouteVisual {
    group: THREE.Group;
    accent: THREE.MeshBasicMaterial;
    bed: THREE.MeshStandardMaterial;
}

/** World units covered by one pulse of the route flow texture. */
const FLOW_LENGTH = 3.4;
let flow: THREE.CanvasTexture | undefined;

/** Repeating comet: a dim line that brightens towards a leading edge. Shared by every solid route. */
function flowTexture() {
    if (!flow) {
        const canvas = document.createElement('canvas');
        canvas.width = 256; canvas.height = 4;
        const context = canvas.getContext('2d')!;
        const gradient = context.createLinearGradient(0, 0, 256, 0);
        gradient.addColorStop(0, 'rgba(255,255,255,.42)');
        gradient.addColorStop(.55, 'rgba(255,255,255,.5)');
        gradient.addColorStop(.92, 'rgba(255,255,255,1)');
        gradient.addColorStop(1, 'rgba(255,255,255,.42)');
        context.fillStyle = gradient;
        context.fillRect(0, 0, 256, 4);
        flow = new THREE.CanvasTexture(canvas);
        flow.wrapS = THREE.RepeatWrapping;
        flow.colorSpace = THREE.SRGBColorSpace;
    }
    return flow;
}

/** Moves the pulses along every solid route. Call once per frame. */
export function advanceRouteFlow(seconds: number) {
    if (flow) flow.offset.x = (flow.offset.x - seconds * .32) % 1;
}

/** A geographic path ribbon: vertices follow the received polyline without smoothing it. */
function ribbon(points: THREE.Vector3[], width: number, y: number, dashed: boolean): THREE.BufferGeometry {
    const vertices: number[] = [];
    const uvs: number[] = [];
    const indices: number[] = [];
    let travelled = 0;
    if (dashed) {
        let distance = 0;
        for (let i = 1; i < points.length; i++) {
            const start = points[i - 1], end = points[i];
            const length = start.distanceTo(end);
            if (length < .0001) continue;
            const side = new THREE.Vector3(-(end.z - start.z), 0, end.x - start.x).normalize().multiplyScalar(width / 2);
            for (let cycle = Math.floor(distance / .85); cycle * .85 < distance + length; cycle++) {
                const along = Math.max(0, cycle * .85 - distance);
                const endAlong = Math.min(length, cycle * .85 + .48 - distance);
                if (endAlong > along) {
                    const a = start.clone().lerp(end, along / length);
                    const b = start.clone().lerp(end, endAlong / length);
                    const n = vertices.length / 3;
                    vertices.push(a.x + side.x, y, a.z + side.z, a.x - side.x, y, a.z - side.z,
                        b.x + side.x, y, b.z + side.z, b.x - side.x, y, b.z - side.z);
                    uvs.push(0, 0, 0, 1, 0, 0, 0, 1);
                    indices.push(n, n + 2, n + 1, n + 1, n + 2, n + 3);
                }
            }
            distance += length;
        }
    } else {
        let previousSide: THREE.Vector3 | undefined;
        for (let i = 1; i < points.length; i++) {
            const start = points[i - 1], end = points[i];
            if (start.distanceToSquared(end) < .00000001) continue;
            const side = new THREE.Vector3(-(end.z - start.z), 0, end.x - start.x).normalize().multiplyScalar(width / 2);
            const n = vertices.length / 3;
            vertices.push(start.x + side.x, y, start.z + side.z, start.x - side.x, y, start.z - side.z,
                end.x + side.x, y, end.z + side.z, end.x - side.x, y, end.z - side.z);
            // u runs along the route so the flow texture travels from origin to destination.
            const u0 = travelled / FLOW_LENGTH, u1 = (travelled += start.distanceTo(end)) / FLOW_LENGTH;
            uvs.push(u0, 0, u0, 1, u1, 0, u1, 1);
            indices.push(n, n + 2, n + 1, n + 1, n + 2, n + 3);
            if (previousSide) {
                const area = previousSide.z * side.x - previousSide.x * side.z;
                if (Math.abs(area) > .00000001) for (const sign of [-1, 1]) {
                    const join = vertices.length / 3;
                    vertices.push(start.x, y, start.z, start.x + previousSide.x * sign, y, start.z + previousSide.z * sign,
                        start.x + side.x * sign, y, start.z + side.z * sign);
                    uvs.push(u0, .5, u0, .5, u0, .5);
                    if (area > 0) indices.push(join, join + 1, join + 2);
                    else indices.push(join, join + 2, join + 1);
                }
            }
            previousSide = side;
        }
    }
    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(vertices, 3));
    geometry.setAttribute('uv', new THREE.Float32BufferAttribute(uvs, 2));
    geometry.setIndex(indices);
    geometry.computeVertexNormals();
    return geometry;
}

export function createRouteVisual(points: THREE.Vector3[], dashed: boolean): RouteVisual {
    const group = new THREE.Group();
    const bed = new THREE.MeshStandardMaterial({ color: '#294b5f', roughness: .85, transparent: true, opacity: .75 });
    const accent = new THREE.MeshBasicMaterial({ color: '#7aa9bd', map: dashed ? null : flowTexture(), transparent: true, opacity: .65, depthWrite: false, toneMapped: false });
    const clean: THREE.Vector3[] = [];
    for (const point of points) if (!clean.length || point.distanceToSquared(clean[clean.length - 1]) > .000001) clean.push(point);
    const last = points[points.length - 1];
    if (last && clean[clean.length - 1].distanceToSquared(last) > 0) clean.push(last);
    if (clean.length > 1) {
        group.add(new THREE.Mesh(ribbon(clean, dashed ? .22 : .38, .026, dashed), bed));
        const highlight = new THREE.Mesh(ribbon(clean, dashed ? .09 : .13, .043, dashed), accent);
        highlight.renderOrder = 1;
        group.add(highlight);
    }
    return { group, accent, bed };
}

export function createPlaceLabel(title: string, subtitle: string, accent = '#69d9bf', selected = false): THREE.Sprite {
    const canvas = document.createElement('canvas');
    canvas.width = 640;
    canvas.height = 160;
    const context = canvas.getContext('2d')!;
    context.fillStyle = 'rgba(12, 30, 44, .94)';
    context.beginPath();
    context.roundRect(2, 2, 636, 156, 18);
    context.fill();
    context.strokeStyle = selected ? accent : '#3d6072';
    context.lineWidth = 3;
    context.stroke();
    context.fillStyle = accent;
    context.fillRect(24, 28, 5, 102);
    context.font = '600 30px Arial, sans-serif';
    context.fillText(subtitle.toLocaleUpperCase('es').slice(0, 35), 49, 58);
    context.fillStyle = '#eef8fc';
    context.font = '600 46px Arial, sans-serif';
    const text = title.length > 24 ? title.slice(0, 23) + '…' : title;
    context.fillText(text, 48, 115, 558);
    const texture = new THREE.CanvasTexture(canvas);
    texture.colorSpace = THREE.SRGBColorSpace;
    texture.minFilter = THREE.LinearFilter;
    texture.generateMipmaps = false;
    const sprite = new THREE.Sprite(new THREE.SpriteMaterial({ map: texture, transparent: true, depthTest: false, depthWrite: false, toneMapped: false, sizeAttenuation: false }));
    // Sprite's default quad is global; a label must own the geometry it disposes.
    sprite.geometry = sprite.geometry.clone();
    sprite.scale.set(.2, .05, 1);
    sprite.userData['selectedLabel'] = selected;
    sprite.renderOrder = selected ? 8 : 5;
    return sprite;
}

/** Top surface of the deck: soft vignette, survey grid and a lit edge, masked to the rounded outline. */
function deckTexture(width: number, depth: number, radius: number) {
    const scale = Math.min(48, 2048 / Math.max(width, depth));
    const canvas = document.createElement('canvas');
    canvas.width = Math.round(width * scale);
    canvas.height = Math.round(depth * scale);
    const context = canvas.getContext('2d')!;
    const w = canvas.width, h = canvas.height;
    context.beginPath();
    context.roundRect(0, 0, w, h, radius * scale);
    context.clip();
    const vignette = context.createRadialGradient(w / 2, h / 2, 0, w / 2, h / 2, Math.hypot(w, h) / 2);
    vignette.addColorStop(0, '#22506a');
    vignette.addColorStop(.6, '#173a50');
    vignette.addColorStop(1, '#0e2434');
    context.fillStyle = vignette;
    context.fillRect(0, 0, w, h);
    for (const [step, alpha] of [[1, .045], [4, .11]]) {
        context.strokeStyle = `rgba(150, 214, 230, ${alpha})`;
        context.lineWidth = 1;
        context.beginPath();
        for (let x = w / 2 % (step * scale); x < w; x += step * scale) { context.moveTo(x, 0); context.lineTo(x, h); }
        for (let y = h / 2 % (step * scale); y < h; y += step * scale) { context.moveTo(0, y); context.lineTo(w, y); }
        context.stroke();
    }
    context.fillStyle = 'rgba(160, 232, 235, .32)';
    for (let x = w / 2 % (4 * scale); x < w; x += 4 * scale) for (let y = h / 2 % (4 * scale); y < h; y += 4 * scale) context.fillRect(x - 1.5, y - 1.5, 3, 3);
    context.strokeStyle = 'rgba(96, 226, 204, .55)';
    context.lineWidth = Math.max(2, scale * .06);
    context.beginPath();
    context.roundRect(scale * .35, scale * .35, w - scale * .7, h - scale * .7, (radius - .35) * scale);
    context.stroke();
    const texture = new THREE.CanvasTexture(canvas);
    texture.colorSpace = THREE.SRGBColorSpace;
    texture.anisotropy = 8;
    return texture;
}

/** A bounded schematic deck; all visible routes and hubs still come from shipment coordinates. */
export function createNetworkStage(width: number, depth: number): THREE.Group {
    const stage = new THREE.Group(), radius = 1.5;
    const base = new THREE.Mesh(slab(width, depth, .55, radius), new THREE.MeshStandardMaterial({ color: '#12293a', roughness: .6, metalness: .45 }));
    base.position.y = -.553;
    base.receiveShadow = true;
    const rim = new THREE.Mesh(slab(width + .34, depth + .34, .1, radius + .17), new THREE.MeshStandardMaterial({ color: '#1c4a58', roughness: .4, metalness: .6, emissive: '#35e0c0', emissiveIntensity: 1.6 }));
    rim.position.y = -.7;
    const surface = new THREE.Mesh(new THREE.PlaneGeometry(width, depth), new THREE.MeshStandardMaterial({ map: deckTexture(width, depth, radius), transparent: true, roughness: .82, metalness: .18 }));
    surface.rotation.x = -Math.PI / 2;
    surface.receiveShadow = true;
    // Glow spilling from under the deck so it floats instead of ending in a hard edge.
    const halo = new THREE.Mesh(new THREE.PlaneGeometry(width * 2.1, depth * 2.1), new THREE.MeshBasicMaterial({ map: glowTexture(), color: '#1aa89a', transparent: true, opacity: .3, depthWrite: false, blending: THREE.AdditiveBlending, toneMapped: false }));
    halo.rotation.x = -Math.PI / 2;
    halo.position.y = -.9;
    stage.add(halo, rim, base, surface);
    return stage;
}

let beam: THREE.CanvasTexture | undefined;
function beamTexture() {
    if (!beam) {
        const canvas = document.createElement('canvas');
        canvas.width = 4; canvas.height = 128;
        const context = canvas.getContext('2d')!;
        const gradient = context.createLinearGradient(0, 0, 0, 128);
        gradient.addColorStop(0, 'rgba(255,255,255,0)');
        gradient.addColorStop(1, 'rgba(255,255,255,1)');
        context.fillStyle = gradient;
        context.fillRect(0, 0, 4, 128);
        beam = new THREE.CanvasTexture(canvas);
        beam.colorSpace = THREE.SRGBColorSpace;
    }
    return beam;
}

/** Marks the recorded coordinate of a hub: light column, ground ring and an expanding pulse (`userData.pulse`). */
export function createBeacon(color: string): { group: THREE.Group; pulse: THREE.Mesh<THREE.RingGeometry, THREE.MeshBasicMaterial> } {
    const group = new THREE.Group();
    const tint = new THREE.Color(color).multiplyScalar(2.4);
    const flat = (geometry: THREE.BufferGeometry, opacity: number, y: number) => {
        const mesh = new THREE.Mesh(geometry, new THREE.MeshBasicMaterial({ color: tint, transparent: true, opacity, side: THREE.DoubleSide, depthWrite: false, toneMapped: false }));
        mesh.rotation.x = -Math.PI / 2;
        mesh.position.y = y;
        group.add(mesh);
        return mesh;
    };
    const column = new THREE.Mesh(new THREE.CylinderGeometry(.13, .045, 3.3, 20, 1, true), new THREE.MeshBasicMaterial({ color: tint, map: beamTexture(), transparent: true, opacity: .6, side: THREE.DoubleSide, depthWrite: false, blending: THREE.AdditiveBlending, toneMapped: false }));
    column.position.y = 1.72;
    group.add(column);
    flat(new THREE.CircleGeometry(.13, 24), .95, .135);
    flat(new THREE.RingGeometry(.3, .38, 40), .9, .13);
    const pulse = flat(new THREE.RingGeometry(.4, .45, 40), .6, .125) as THREE.Mesh<THREE.RingGeometry, THREE.MeshBasicMaterial>;
    return { group, pulse };
}

/** Vertical dusk gradient used as the scene background. */
export function createSkyTexture() {
    const canvas = document.createElement('canvas');
    canvas.width = 4; canvas.height = 512;
    const context = canvas.getContext('2d')!;
    const gradient = context.createLinearGradient(0, 0, 0, 512);
    gradient.addColorStop(0, '#050c15');
    gradient.addColorStop(.45, '#0a1b2a');
    gradient.addColorStop(.72, '#12384a');
    gradient.addColorStop(1, '#081724');
    context.fillStyle = gradient;
    context.fillRect(0, 0, 4, 512);
    const texture = new THREE.CanvasTexture(canvas);
    texture.colorSpace = THREE.SRGBColorSpace;
    return texture;
}
