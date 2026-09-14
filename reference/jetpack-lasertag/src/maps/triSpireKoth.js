export const TriSpireKoth = {
  name: "Tri-Spire KOTH",
  spawnPoints: [{ x: 0, y: 0, z: 15 }],
  
  // Three towering structures forming a highly contested central triangle
  platforms: [
    { x: -10, y: 5.0, z: -6, sx: 5, sz: 5 },  // Spire A Base
    { x: 10, y: 5.0, z: -6, sx: 5, sz: 5 },   // Spire B Base
    { x: 0, y: 8.0, z: 8, sx: 6, sz: 6 },     // Spire C (High Apex)
    { x: 0, y: 1.0, z: -2, sx: 8, sz: 8 }     // Contested Low Center Hill
  ],
  
  // Outer line-of-sight blockers forcing strategic aerial flanks
  pillars: [
    { x: -10, z: -6, radius: 2.0, height: 20 },
    { x: 10, z: -6, radius: 2.0, height: 20 },
    { x: 0, z: 8, radius: 2.5, height: 22 }
  ],
  
  // Aggressive inbound launch vectoring
  launchPads: [
    { x: -8, y: 0.05, z: 6, power: 24 },
    { x: 8, y: 0.05, z: 6, power: 24 },
    { x: 0, y: 0.05, z: -12, power: 25 }
  ],
  
  // Safe perimeter base gates
  rechargePads: [
    { x: -18, y: 0.05, z: 12 }, 
    { x: 18, y: 0.05, z: 12 }
  ],
  
  // Distributed patrol routing anchoring the triangle
  targetSpawns: [
    { x: -10, y: 5.2, z: -6 },
    { x: 10, y: 5.2, z: -6 },
    { x: 0, y: 8.2, z: 8 },
    { x: 0, y: 1.2, z: -2 },
    { x: -12, y: 0.2, z: 0 },
    { x: 12, y: 0.2, z: 0 }
  ]
};