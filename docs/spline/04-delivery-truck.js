// Rigid delivery truck faces +Z. All parts are grouped in the next batch.
const parts = [
  ['Chassis', 128, 18, 432, 470, 56, 620, '#24394b'],
  ['Cargo', 130, 142, 284, 470, 146, 554, '#e2e9ec'],
  ['Cab', 132, 112, 114, 470, 125, 760, '#36cbb0'],
  ['Cab Roof', 134, 8, 116, 470, 186, 760, '#66d7c1'],
  ['Windshield', 112, 43, 4, 470, 155, 819, '#163749'],
  ['Window Left', 4, 39, 65, 402, 157, 766, '#163749'],
  ['Window Right', 4, 39, 65, 538, 157, 766, '#163749'],
  ['Bumper', 140, 15, 12, 470, 66, 823, '#718f9e'],
  ['Grille', 60, 24, 5, 470, 100, 820, '#223d4e'],
  ['Stripe Left', 4, 10, 280, 403, 135, 554, '#36cbb0'],
  ['Stripe Right', 4, 10, 280, 537, 135, 554, '#36cbb0'],
  ['Rear Doors', 120, 126, 4, 470, 146, 410, '#9eafb9'],
  ['Rear Seam', 3, 112, 5, 470, 146, 406, '#536e7c'],
  ['Mirror Left', 10, 23, 10, 389, 159, 803, '#24394b'],
  ['Mirror Right', 10, 23, 10, 551, 159, 803, '#24394b']
];
for (const p of parts) {
  mesh('Cube', { width: p[1], height: p[2], depth: p[3], cornerRadius: p[0] === 'Cab' ? 3 : 1 });
  position({ x: p[4], y: p[5], z: p[6] });
  color(p[7]);
  shading({ category: 'physical', roughness: p[0].includes('Window') || p[0] === 'Windshield' ? 0.18 : 0.55, metalness: 0.22 });
  rename('Delivery Truck ' + p[0]);
  addEvent('MouseDown');
}
for (const x of [402, 538]) {
  for (const z of [465, 545, 755]) {
    mesh('Cylinder', { radius: 27, height: 18, cornerRadius: 0 });
    rotation({ z: 90 });
    position({ x: x, y: 27, z: z });
    color('#122330');
    shading({ category: 'physical', roughness: 0.95 });
    rename('Delivery Truck Tire ' + x + ' ' + z);
    mesh('Cylinder', { radius: 14, height: 20, cornerRadius: 0 });
    rotation({ z: 90 });
    position({ x: x, y: 27, z: z });
    color('#8da6b2');
    shading({ category: 'physical', roughness: 0.4, metalness: 0.75 });
    rename('Delivery Truck Hub ' + x + ' ' + z);
  }
}
for (const x of [423, 517]) {
  mesh('Cube', { width: 18, height: 13, depth: 5, cornerRadius: 1 });
  position({ x: x, y: 103, z: 823 });
  color('#fff0c5');
  shading({ category: 'phong', emissive: '#f3d594' });
  rename('Delivery Truck Headlamp ' + x);
}
for (let i = 0; i < 9; i++) {
  mesh('Cube', { width: 132, height: 2, depth: 4, cornerRadius: 0 });
  position({ x: 470, y: 100 + i * 13, z: 696 });
  color('#b6c7d0');
  rename('Delivery Truck Body Rib ' + i);
}
