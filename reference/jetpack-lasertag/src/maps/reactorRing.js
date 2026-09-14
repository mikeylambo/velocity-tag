export const ReactorRing = {
  name: "Reactor Ring",
  spawnPoints: [{ x: 0, y: 0, z: 16 }],
  
  // Elevated outer perimeter bridges enclosing a hazardous inner core path
  platforms: [
    { x: -14, y: 4.0, z: 0, sx: 6, sz: 12 }, // West Bridge
    { x: 14, y: 4.0, z: 0, sx: 6, sz: 12 },  // East Bridge
    { x: 0, y: 8.0, z: -14, sx: 10, sz: 6 }, // North Overlook Apex
    { x: 0, y: 1.0, z: 0, sx: 12, sz: 12 }   // Exposed Low Center Core
  ],
  
  // Massive line-of-sight blockers forcing ring-lane rotations
  pillars: [
    { x: 0, z: 0, radius: 4.5, height: 20 },   // Massive Central Reactor Column
    { x: -14, z: -10, radius: 2.0, height: 18 },
    { x: 14, z: -10, radius: 2.0, height: 18 }
  ],
  
  // High-velocity orbital insertion points
  launchPads: [
    { x: -14, y: 0.05, z: 10, power: 23 },
    { x: 14, y: 0.05, z: 10, power: 23 },
    { x: 0, y: 0.05, z: -8, power: 26 }
  ],
  
  // Protected perimeter sync stations
  rechargePads: [
    { x: -20, y: 0.05, z: 0 },
    { x: 20, y: 0.05, z: 0 }
  ],
  
  // Distributed orbital routing lanes
  targetSpawns: [
    { x: -14, y: 4.2, z: 0 },
    { x: 14, y: 4.2, z: 0 },
    { x: 0, y: 8.2, z: -14 },
    { x: -8, y: 1.2, z: -8 },
    { x: 8, y: 1.2, z: -8 },
    { x: 0, y: 1.2, z: 8 }
  ]
};