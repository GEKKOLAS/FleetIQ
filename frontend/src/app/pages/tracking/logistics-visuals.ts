import * as THREE from 'three';

export interface RouteVisual {
    group: THREE.Group;
    accent: THREE.MeshBasicMaterial;
    bed: THREE.MeshStandardMaterial;
}

/** A geographic path ribbon: vertices follow the received polyline without smoothing it. */
function ribbon(points: THREE.Vector3[], width: number, y: number, dashed: boolean): THREE.BufferGeometry {
    const vertices: number[] = [];
    const indices: number[] = [];
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
            indices.push(n, n + 2, n + 1, n + 1, n + 2, n + 3);
            if (previousSide) {
                const area = previousSide.z * side.x - previousSide.x * side.z;
                if (Math.abs(area) > .00000001) for (const sign of [-1, 1]) {
                    const join = vertices.length / 3;
                    vertices.push(start.x, y, start.z, start.x + previousSide.x * sign, y, start.z + previousSide.z * sign,
                        start.x + side.x * sign, y, start.z + side.z * sign);
                    if (area > 0) indices.push(join, join + 1, join + 2);
                    else indices.push(join, join + 2, join + 1);
                }
            }
            previousSide = side;
        }
    }
    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(vertices, 3));
    geometry.setIndex(indices);
    geometry.computeVertexNormals();
    return geometry;
}

export function createRouteVisual(points: THREE.Vector3[], dashed: boolean): RouteVisual {
    const group = new THREE.Group();
    const bed = new THREE.MeshStandardMaterial({ color: '#294b5f', roughness: .85, transparent: true, opacity: .75 });
    const accent = new THREE.MeshBasicMaterial({ color: '#7aa9bd', transparent: true, opacity: .65, depthWrite: false, toneMapped: false });
    const clean: THREE.Vector3[] = [];
    for (const point of points) if (!clean.length || point.distanceToSquared(clean[clean.length - 1]) > .000001) clean.push(point);
    const last = points[points.length - 1];
    if (last && clean[clean.length - 1].distanceToSquared(last) > 0) clean.push(last);
    if (clean.length > 1) {
        group.add(new THREE.Mesh(ribbon(clean, dashed ? .22 : .38, .026, dashed), bed));
        const highlight = new THREE.Mesh(ribbon(clean, dashed ? .09 : .10, .043, dashed), accent);
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

/** A bounded schematic deck; all visible routes and hubs still come from shipment coordinates. */
export function createNetworkStage(width: number, depth: number): THREE.Group {
    const stage = new THREE.Group();
    const deck = new THREE.Mesh(new THREE.BoxGeometry(width, .36, depth), new THREE.MeshStandardMaterial({ color: '#173345', roughness: .86, metalness: .12 }));
    deck.position.y = -.18;
    deck.receiveShadow = true;
    stage.add(deck);
    const trim = new THREE.Mesh(new THREE.BoxGeometry(width + .22, .14, depth + .22), new THREE.MeshStandardMaterial({ color: '#285165', roughness: .65, metalness: .5 }));
    trim.position.y = -.41;
    stage.add(trim);
    const grid: number[] = [];
    for (let x = -Math.floor(width / 4) * 2; x < width / 2; x += 2) grid.push(x, .007, -depth / 2, x, .007, depth / 2);
    for (let z = -Math.floor(depth / 4) * 2; z < depth / 2; z += 2) grid.push(-width / 2, .007, z, width / 2, .007, z);
    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(grid, 3));
    stage.add(new THREE.LineSegments(geometry, new THREE.LineBasicMaterial({ color: '#487085', transparent: true, opacity: .15, depthWrite: false })));
    const border = new THREE.BufferGeometry().setFromPoints([
        new THREE.Vector3(-width / 2 + .4, .012, -depth / 2 + .4), new THREE.Vector3(width / 2 - .4, .012, -depth / 2 + .4),
        new THREE.Vector3(width / 2 - .4, .012, depth / 2 - .4), new THREE.Vector3(-width / 2 + .4, .012, depth / 2 - .4)
    ]);
    stage.add(new THREE.LineLoop(border, new THREE.LineBasicMaterial({ color: '#4d8b91', transparent: true, opacity: .45 })));
    return stage;
}
