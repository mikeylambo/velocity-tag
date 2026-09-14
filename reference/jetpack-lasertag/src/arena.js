import * as THREE from 'three';
import { CONFIG } from './config.js';
import { EventBus } from './eventBus.js';
import { TrainingCylinder } from './maps/trainingCylinder.js';

// Safely attempt dynamic multi-map support assuming standard repository schema layouts
let TriSpireKoth = null;
let ReactorRing = null;
try {
  TriSpireKoth = require('./maps/triSpireKoth.js').TriSpireKoth;
  ReactorRing = require('./maps/reactorRing.js').ReactorRing;
} catch (e) { /* Fallback gracefully to core mapping primitives */ }

export class Arena {
  constructor(scene) {
    this.scene = scene;
    this.obstacles = [];
    this.platforms = [];
    this.launchPads = [];
    this.rechargePads = [];
    this.assetMeshGroups = [];
    
    // Internal trigger debouncing cache
    this.triggerMem = { launch: 0, sync: 0 };

    // Set Map Reference System
    this.mapData = TrainingCylinder;

    this.matFloor = new THREE.MeshStandardMaterial({ color: 0x050811, roughness: 0.9 });
    this.matWall = new THREE.MeshBasicMaterial({ color: 0x07132a, transparent: true, opacity: 0.35, side: THREE.DoubleSide });
    this.matObstacle = new THREE.MeshStandardMaterial({ color: 0x0b1022, roughness: 0.4 });
    this.matPillar = new THREE.MeshStandardMaterial({ color: 0x080d1a, roughness: 0.6 });

    this.buildBase();
    this.loadMapSchema(TrainingCylinder);
    this.bindEvents();
  }

  buildBase() {
    const floor = new THREE.Mesh(new THREE.CylinderGeometry(CONFIG.ARENA_RADIUS, CONFIG.ARENA_RADIUS, 0.5, 64), this.matFloor);
    floor.position.y = -0.25;
    this.scene.add(floor);

    const grid = new THREE.GridHelper(CONFIG.ARENA_RADIUS * 2, 40, 0x00eaff, 0x073060);
    grid.position.y = 0.01;
    this.scene.add(grid);

    const wall = new THREE.Mesh(new THREE.CylinderGeometry(CONFIG.ARENA_RADIUS, CONFIG.ARENA_RADIUS, CONFIG.ARENA_HEIGHT, 64, 1, true), this.matWall);
    wall.position.y = CONFIG.ARENA_HEIGHT / 2;
    this.scene.add(wall);
  }

  bindEvents() {
    EventBus.on('ROUND_START', ({ map }) => {
      if (map === 'triSpireKoth' && TriSpireKoth) {
        this.loadMapSchema(TriSpireKoth);
      } else if (map === 'reactorRing' && ReactorRing) {
        this.loadMapSchema(ReactorRing);
      } else {
        this.loadMapSchema(TrainingCylinder);
      }
    });
  }

  clearCurrentMap() {
    this.assetMeshGroups.forEach(mesh => {
      mesh.removeFromParent();
      if (mesh.geometry) mesh.geometry.dispose();
    });
    this.assetMeshGroups = [];
    this.obstacles = [];
    this.platforms = [];
    this.launchPads = [];
    this.rechargePads = [];
  }

  loadMapSchema(targetSchema = TrainingCylinder) {
    this.clearCurrentMap();
    this.mapData = targetSchema;

    // 1. Generate Mapped Platforms
    if (this.mapData.platforms) {
      this.mapData.platforms.forEach(p => {
        const mesh = new THREE.Mesh(new THREE.BoxGeometry(p.sx, 0.4, p.sz), this.matObstacle);
        mesh.position.set(p.x, p.y, p.z);
        this.scene.add(mesh);
        
        this.assetMeshGroups.push(mesh);
        this.obstacles.push(mesh);
        // Track visual upper bounds accurately: height is 0.4, top face sits at p.y + 0.2
        this.platforms.push({ 
          y: p.y + 0.2, 
          minX: p.x - p.sx/2, 
          maxX: p.x + p.sx/2, 
          minZ: p.z - p.sz/2, 
          maxZ: p.z + p.sz/2 
        });

        const edges = new THREE.LineSegments(
          new THREE.EdgesGeometry(mesh.geometry), 
          new THREE.LineBasicMaterial({ color: 0x00eaff, blending: THREE.AdditiveBlending })
        );
        edges.position.copy(mesh.position);
        this.scene.add(edges);
        this.assetMeshGroups.push(edges);
      });
    }

    // 2. Generate Structural Pillars
    if (this.mapData.pillars) {
      this.mapData.pillars.forEach(p => {
        const mesh = new THREE.Mesh(new THREE.CylinderGeometry(p.radius, p.radius, p.height, 32), this.matPillar);
        mesh.position.set(p.x, p.height / 2, p.z);
        this.scene.add(mesh);
        this.assetMeshGroups.push(mesh);
        this.obstacles.push(mesh);
      });
    }

    // 3. Generate High-Speed Launch Pads
    if (this.mapData.launchPads) {
      this.mapData.launchPads.forEach(lp => {
        const group = new THREE.Group();
        group.position.set(lp.x, lp.y, lp.z);
        
        const ring = new THREE.Mesh(
          new THREE.RingGeometry(0.8, 1.4, 16), 
          new THREE.MeshBasicMaterial({ color: 0xff6600, side: THREE.DoubleSide, blending: THREE.AdditiveBlending })
        );
        ring.rotation.x = Math.PI / 2;
        group.add(ring);

        const arrow = new THREE.Mesh(
          new THREE.ConeGeometry(0.5, 1.0, 8),
          new THREE.MeshBasicMaterial({ color: 0xffd76a, wireframe: true, blending: THREE.AdditiveBlending })
        );
        arrow.position.y = 0.6;
        group.add(arrow);

        this.scene.add(group);
        this.assetMeshGroups.push(group);
        this.launchPads.push({ x: lp.x, z: lp.z, power: lp.power });
      });
    }

    // 4. Generate Suit Sync Base Gates
    if (this.mapData.rechargePads) {
      this.mapData.rechargePads.forEach(rp => {
        const group = new THREE.Group();
        group.position.set(rp.x, rp.y, rp.z);

        const base = new THREE.Mesh(
          new THREE.BoxGeometry(2.2, 0.1, 2.2),
          new THREE.MeshBasicMaterial({ color: 0x00ff66, wireframe: true, blending: THREE.AdditiveBlending })
        );
        group.add(base);

        this.scene.add(group);
        this.assetMeshGroups.push(group);
        this.rechargePads.push({ x: rp.x, z: rp.z });
      });
    }
  }

  getFloorY(playerPos) {
    let groundY = 0;
    for (const plat of this.platforms) {
      if (playerPos.x > plat.minX && playerPos.x < plat.maxX && playerPos.z > plat.minZ && playerPos.z < plat.maxZ) {
        // Enforce landing catch strictly down to 0.25m below top surface bounds
        if (playerPos.y >= plat.y - 0.25 && playerPos.y < plat.y + 1.5) {
          groundY = Math.max(groundY, plat.y);
        }
      }
    }
    return groundY;
  }

  // BULLETPROOF 3D VOLUME COLLISION RESOLVER
  resolveCollisions(pos, velocity = null, radius = 0.4) {
    // 1. Resolve Structural Cylindrical Pillars
    if (this.mapData && this.mapData.pillars) {
      this.mapData.pillars.forEach(p => {
        const dist = Math.hypot(pos.x - p.x, pos.z - p.z);
        const minSafeDist = p.radius + radius;
        if (dist < minSafeDist && pos.y < p.height) {
          const angle = Math.atan2(pos.z - p.z, pos.x - p.x);
          pos.x = p.x + Math.cos(angle) * minSafeDist;
          pos.z = p.z + Math.sin(angle) * minSafeDist;
        }
      });
    }

    // 2. Resolve Solid Platform Slabs (Top, Bottom Ceiling, and Side Walls)
    const playerHeight = 1.6; // Upper head boundary profile offset above local foot origin

    this.platforms.forEach(plat => {
      const platTop = plat.y;
      const platBottom = plat.y - 0.4; // Box height is 0.4m

      // Expand boundary testing footprint outward by operational chassis safety radii
      const minX = plat.minX - radius;
      const maxX = plat.maxX + radius;
      const minZ = plat.minZ - radius;
      const maxZ = plat.maxZ + radius;

      // Evaluate whether horizontal spatial coordinates overlap the platform volume column
      if (pos.x > minX && pos.x < maxX && pos.z > minZ && pos.z < maxZ) {
        // Check if chassis sits strictly below the standard top floor landing snap window
        if (pos.y < platTop - 0.25) {
          // CEILING & WALL IMPACT DETECTION: Does upper body structure strike the slab?
          if (pos.y + playerHeight > platBottom) {
            // Determine minimal axis penetration depth to resolve spatial displacement naturally
            const insetX = Math.min(pos.x - minX, maxX - pos.x);
            const insetZ = Math.min(pos.z - minZ, maxZ - pos.z);
            const insetY = (pos.y + playerHeight) - platBottom;

            // Displace coordinates outward along the axis of least resistance
            if (insetY < insetX && insetY < insetZ) {
              // Direct Ceiling Impact: Halt upward translation safely below bottom face
              pos.y = platBottom - playerHeight;
              if (velocity && velocity.y > 0) {
                velocity.y = 0; // Nullify vertical propulsion vector instantly
              }
              EventBus.emit('LOG_DEBUG', { message: 'CHASSIS IMPACT: Ceiling Slab' });
            } else {
              // Lateral Edge Wall Slider Displacement
              if (insetX < insetZ) {
                pos.x = (pos.x - minX < maxX - pos.x) ? minX : maxX;
              } else {
                pos.z = (pos.z - minZ < maxZ - pos.z) ? minZ : maxZ;
              }
            }
          }
        }
      }
    });
  }

  update(playerAvatar, dt) {
    const now = performance.now() / 1000;
    const pos = playerAvatar.group.position;

    // 1. Process Launch Pad Ejection Overlaps
    if (now - this.triggerMem.launch > CONFIG.LAUNCH_PAD_COOLDOWN) {
      for (const pad of this.launchPads) {
        if (Math.hypot(pos.x - pad.x, pos.z - pad.z) < 1.5 && pos.y < this.getFloorY(pos) + 1.8) {
          playerAvatar.velocity.y = pad.power;
          this.triggerMem.launch = now;
          EventBus.emit('LOG_DEBUG', { message: 'LAUNCH ROUTE ENGAGED' });
          break;
        }
      }
    }

    // 2. Process Physical Base Gate Synchronization Overlaps
    if (now - this.triggerMem.sync > CONFIG.SYNC_PAD_COOLDOWN) {
      for (const gate of this.rechargePads) {
        if (Math.hypot(pos.x - gate.x, pos.z - gate.z) < 1.8 && pos.y < 1.5) {
          this.triggerMem.sync = now;
          EventBus.emit('SUIT_SYNC_TICK');
          EventBus.emit('LOG_DEBUG', { message: 'BASE GATE: SUIT SYNCING...' });
          break;
        }
      }
    }
  }
}