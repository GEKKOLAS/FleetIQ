// Control desk, loading portal and architectural detail.
select(o => o.name === 'Warehouse Brand' || /^Zone [1-5] (Label|Number)$/.test(o.name));
scale({ x: 3.125, y: 3.125, z: 3.125 });
shading({ category: 'phong', emissive: '#a9e3db', side: 'double' });
select('Warehouse Brand');
geometry({ fontSize: 64 });
select(o => o.name.startsWith('Demo Cargo'));
translate({ x: 95, z: -35 });
group();
rename('Demo Cargo');
select(o => o.name.startsWith('Staging'));
group();
rename('Staging Goods');
for (const z of [-455, 365]) {
  mesh('Cube', { width: 1540, height: 16, depth: 14, cornerRadius: 0 });
  position({ x: 0, y: 338, z: z });
  applyMaterial('FleetIQ Steel');
  rename('Warehouse Roof Beam ' + z);
}
const desk = [
  ['Top', 250, 12, 110, 500, 130, -310, '#e0e8eb'],
  ['Leg Left', 12, 70, 85, 390, 89, -310, '#344f62'],
  ['Leg Right', 12, 70, 85, 610, 89, -310, '#344f62'],
  ['Screen Frame', 155, 85, 10, 500, 188, -340, '#182f40'],
  ['Screen Glass', 142, 72, 3, 500, 188, -332, '#143e50'],
  ['Stand', 15, 20, 15, 500, 145, -340, '#344f62'],
  ['Keyboard', 96, 4, 28, 500, 139, -282, '#263f52']
];
for (const p of desk) {
  mesh('Cube', { width: p[1], height: p[2], depth: p[3], cornerRadius: 1 });
  position({ x: p[4], y: p[5], z: p[6] });
  color(p[7]);
  shading({ category: 'physical', roughness: 0.6, metalness: 0.2 });
  rename('Control Desk ' + p[0]);
}
mesh('Text', { text: 'LOAD / ROUTE\nCARRIER VERIFIED', font: 'JetBrains Mono_500', fontSize: 10, depth: 0.5 });
position({ x: 500, y: 190, z: -328 });
color('#9af1d9');
shading({ category: 'phong', emissive: '#9af1d9', side: 'double' });
rename('Control Desk Screen Content');
for (const x of [370, 570]) {
  mesh('Cube', { width: 16, height: 244, depth: 22, cornerRadius: 0 });
  position({ x: x, y: 172, z: 380 });
  applyMaterial('FleetIQ Steel');
  rename('Loading Dock Portal ' + x);
}
mesh('Cube', { width: 220, height: 18, depth: 24, cornerRadius: 0 });
position({ x: 470, y: 301, z: 380 });
applyMaterial('FleetIQ Mint');
rename('Loading Dock Lintel');
mesh('Cube', { width: 182, height: 88, depth: 9, cornerRadius: 0 });
position({ x: 470, y: 250, z: 380 });
color('#7894a3');
shading({ category: 'physical', roughness: 0.45, metalness: 0.5 });
rename('Dock Shutter');
for (const x of [355, 585]) {
  mesh('Cylinder', { radius: 7, height: 65, cornerRadius: 0 });
  position({ x: x, y: 35, z: 420 });
  color('#d7b96f');
  rename('Dock Bollard ' + x);
}
mesh('Cube', { width: 164, height: 8, depth: 75, cornerRadius: 0 });
position({ x: 470, y: 52, z: 400 });
color('#6f899a');
rename('Loading Dock Bridge');
for (const x of [350, 590]) {
  mesh('Cube', { width: 4, height: 2, depth: 660, cornerRadius: 0 });
  position({ x: x, y: 1, z: 760 });
  color('#789097');
  rename('Yard Lane Boundary ' + x);
}
for (let i = 0; i < 7; i++) {
  mesh('Cube', { width: 28, height: 2, depth: 60, cornerRadius: 0 });
  position({ x: 255, y: 1, z: 500 + i * 85 });
  color('#4e7f82');
  rename('Yard Route Marker ' + i);
}
for (let i = 0; i < 2; i++) {
  mesh('Cube', { width: 76, height: 82, depth: 24, cornerRadius: 2 });
  position({ x: -730 + i * 110, y: 95, z: 300 });
  color(i === 0 ? '#6d9e94' : '#748aa1');
  rename('Warehouse Safety Station ' + i);
}
lookFrom({ azimuth: -24, elevation: 37 });
