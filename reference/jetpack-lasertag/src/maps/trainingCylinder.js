export const TrainingCylinder = {
  name: "Training Cylinder",
  spawnPoints: [{ x: 0, y: 0, z: 12 }],
  platforms: [
    { x: 0, y: 4.0, z: -8, sx: 8, sz: 4 },
    { x: -12, y: 8.0, z: 4, sx: 6, sz: 4 },
    { x: 12, y: 8.0, z: 4, sx: 6, sz: 4 },
    { x: 0, y: 12.0, z: 8, sx: 5, sz: 5 }
  ],
  pillars: [{ x: 0, z: 0, radius: 3.0, height: 18 }],
  launchPads: [
    { x: 0, y: 0.05, z: -16, power: 22 },
    { x: 0, y: 0.05, z: 16, power: 22 },
    { x: -16, y: 0.05, z: 0, power: 22 },
    { x: 16, y: 0.05, z: 0, power: 22 }
  ],
  rechargePads: [{ x: -21, y: 0.05, z: -5 }, { x: 21, y: 0.05, z: -5 }],
  targetSpawns: [
    { x: 0, y: 0.2, z: -10 }, { x: -12, y: 4.2, z: -8 }, { x: 12, y: 4.2, z: -8 },
    { x: -12, y: 8.2, z: 4 }, { x: 12, y: 8.2, z: 4 }, { x: 0, y: 12.2, z: 8 }
  ]
};