// Spline editor DSL. Run batches in numeric order in FleetIQ Control Tower.
mesh('Cube', { width: 2100, height: 40, depth: 1820, cornerRadius: 12 });
position({ x: 0, y: -23, z: 260 });
color('#142b3d');
shading({ category: 'physical', roughness: 0.82, metalness: 0.08 });
createMaterial('FleetIQ Asphalt');
rename('Warehouse Site');
mesh('Cube', { width: 1600, height: 50, depth: 880, cornerRadius: 0 });
position({ x: 0, y: 25, z: -50 });
color('#526b7a');
shading({ category: 'physical', roughness: 0.9, metalness: 0.05 });
createMaterial('FleetIQ Concrete');
rename('Warehouse Floor');
mesh('Cube', { width: 1600, height: 280, depth: 14, cornerRadius: 0 });
position({ x: 0, y: 190, z: -490 });
color('#c2d0d7');
shading({ category: 'physical', roughness: 0.65, metalness: 0.2 });
createMaterial('FleetIQ Cladding');
rename('Warehouse Rear Wall');
for (const x of [-793, 793]) {
  mesh('Cube', { width: 14, height: 74, depth: 864, cornerRadius: 0 });
  position({ x: x, y: 87, z: -50 });
  applyMaterial('FleetIQ Cladding');
  rename('Warehouse Cutaway Side ' + x);
}
mesh('Cube', { width: 1604, height: 22, depth: 20, cornerRadius: 0 });
position({ x: 0, y: 302, z: -477 });
color('#36cbb0');
shading({ category: 'physical', roughness: 0.5, metalness: 0.2 });
createMaterial('FleetIQ Mint');
rename('Warehouse Mint Fascia');
for (const x of [-775, -260, 260, 775]) {
  for (const z of [-470, 365]) {
    mesh('Cube', { width: 14, height: 290, depth: 14, cornerRadius: 0 });
    position({ x: x, y: 195, z: z });
    color('#243e51');
    shading({ category: 'physical', roughness: 0.45, metalness: 0.65 });
    if (x === -775 && z === -470) createMaterial('FleetIQ Steel');
    else applyMaterial('FleetIQ Steel');
    rename('Warehouse Column ' + x + ' ' + z);
  }
}
for (let i = 0; i < 31; i++) {
  mesh('Cube', { width: 3, height: 222, depth: 4, cornerRadius: 0 });
  position({ x: -750 + i * 50, y: 180, z: -480 });
  applyMaterial('FleetIQ Cladding');
  rename('Warehouse Cladding Rib ' + i);
}
mesh('Cube', { width: 1640, height: 20, depth: 920, cornerRadius: 0 });
position({ x: 0, y: 352, z: -50 });
color('#344f62');
shading({ category: 'physical', roughness: 0.55, metalness: 0.5 });
createMaterial('FleetIQ Roof');
rename('Warehouse Roof');
hide();
mesh('Cube', { width: 460, height: 64, depth: 8, cornerRadius: 2 });
position({ x: 0, y: 245, z: -475 });
color('#112b3c');
rename('Warehouse Brand Panel');
mesh('Text', { text: 'FLEETIQ', font: 'Space Grotesk_700', fontSize: 49, depth: 1 });
scale({ x: 0.32, y: 0.32, z: 0.32 });
position({ x: 0, y: 245, z: -458 });
color('#8bf5d9');
shading({ category: 'phong', emissive: '#36cbb0' });
rename('Warehouse Brand');
select(o => o.isLight);
updateLight({ intensity: 1.05, color: '#f0f6ff', size: 3000, shadows: true, shadowRadius: 3 });
position({ x: -650, y: 1300, z: 950 });
rotation({ x: -50, y: -30, z: 0 });
background('#0b1b29');
environment({ ao: true, ambient: { color: '#b8d4ee', intensity: 0.72 } });
fog({ useBackgroundColor: true, near: 5000, far: 11000 });
lookFrom({ azimuth: -30, elevation: 34 });
