// Five conceptual transport workflow zones; these are not inventory entities.
const zones = [
  { n: 1, name: 'PEDIDOS', x: -525, z: -160, w: 410, d: 490, c: '#27566a' },
  { n: 2, name: 'CONSOLIDACION', x: -40, z: -160, w: 430, d: 490, c: '#285b61' },
  { n: 3, name: 'PLANIFICACION', x: 500, z: -270, w: 430, d: 280, c: '#334e70' },
  { n: 4, name: 'DESPACHO', x: 465, z: 160, w: 470, d: 350, c: '#326b65' },
  { n: 5, name: 'SEGUIMIENTO', x: -350, z: 790, w: 550, d: 270, c: '#233f59' }
];
for (const zone of zones) {
  mesh('Cube', { width: zone.w, height: 2, depth: zone.d, cornerRadius: 8 });
  position({ x: zone.x, y: zone.n === 5 ? -1 : 52, z: zone.z });
  color(zone.c);
  shading({ category: 'physical', roughness: 0.85 });
  rename('Zone ' + zone.n + ' Pad');
  addEvent('MouseDown');
  mesh('Cylinder', { radius: 32, height: 8, cornerRadius: 0 });
  position({ x: zone.x - zone.w / 2 + 50, y: zone.n === 5 ? 8 : 64, z: zone.z + zone.d / 2 - 55 });
  color('#64f6cf');
  shading({ category: 'phong', emissive: '#184d49' });
  rename('Zone ' + zone.n + ' Target');
  addEvent('MouseDown');
  mesh('Text', { text: String(zone.n).padStart(2, '0'), font: 'JetBrains Mono_700', fontSize: 30, depth: 1 });
  scale({ x: 0.32, y: 0.32, z: 0.32 });
  rotation({ x: -90 });
  position({ x: zone.x - zone.w / 2 + 50, y: zone.n === 5 ? 14 : 70, z: zone.z + zone.d / 2 - 55 });
  color('#0d2637');
  rename('Zone ' + zone.n + ' Number');
  mesh('Text', { text: zone.name, font: 'Space Grotesk_500', fontSize: 23, depth: 1 });
  scale({ x: 0.32, y: 0.32, z: 0.32 });
  rotation({ x: -90 });
  position({ x: zone.x + 40, y: zone.n === 5 ? 7 : 58, z: zone.z + zone.d / 2 - 55 });
  color('#d7eeed');
  shading({ category: 'phong', emissive: '#447279' });
  rename('Zone ' + zone.n + ' Label');
}
for (const x of [-640, -500, -360, -160, 0, 160]) {
  const z = x < -300 ? -165 : -190;
  mesh('Cube', { width: 105, height: 13, depth: 100, cornerRadius: 0 });
  position({ x: x, y: 60, z: z });
  color('#977858');
  shading({ category: 'physical', roughness: 0.94 });
  rename('Staging Pallet ' + x);
  for (let j = 0; j < 3; j++) {
    mesh('Cube', { width: 94, height: 4, depth: 4, cornerRadius: 0 });
    position({ x: x, y: 69, z: z - 34 + j * 34 });
    color('#b6936a');
    rename('Staging Pallet Slat ' + x + ' ' + j);
  }
  for (let j = 0; j < (x === -500 || x === 0 ? 2 : 1); j++) {
    mesh('Cube', { width: 81, height: 48, depth: 78, cornerRadius: 1 });
    position({ x: x, y: 96 + j * 50, z: z });
    color(x < -300 ? '#c2a482' : '#9cb9c3');
    shading({ category: 'physical', roughness: 0.85 });
    rename('Staging Parcel ' + x + ' ' + j);
    mesh('Cube', { width: 9, height: 3, depth: 80, cornerRadius: 0 });
    position({ x: x, y: 122 + j * 50, z: z });
    color('#e5ddc5');
    rename('Staging Strap ' + x + ' ' + j);
  }
}
mesh('Cube', { width: 110, height: 14, depth: 100, cornerRadius: 0 });
position({ x: -630, y: 61, z: 18 });
color('#a38564');
rename('Demo Cargo Pallet');
mesh('Cube', { width: 90, height: 60, depth: 80, cornerRadius: 1 });
position({ x: -630, y: 100, z: 18 });
applyMaterial('FleetIQ Mint');
rename('Demo Cargo Box');
mesh('Cube', { width: 10, height: 3, depth: 82, cornerRadius: 0 });
position({ x: -630, y: 132, z: 18 });
color('#c8f6e8');
rename('Demo Cargo Strap');
