import * as THREE from 'three';
import { CONFIG } from './config.js';

export class Arena {
  constructor(scene) {
    this.scene = scene;
    this.obstacles = [];
    this.platforms = [];
    this.rechargePads = [];
    this.launchPads = [];
    this.animatables = [];

    this.matFloor = new THREE.MeshStandardMaterial({ color: 0x050811, roughness: 0.9 });
    this.matWall = new THREE.MeshBasicMaterial({ color: 0x07132a, transparent: true, opacity: 0.35, side: THREE.DoubleSide });
    this.matObstacle = new THREE.MeshStandardMaterial({ color: 0x0b1022, roughness: 0.42, metalness: 0.38 });
    this.matCyanGlow = new THREE.MeshBasicMaterial({ color: 0x00eaff, blending: THREE.AdditiveBlending, transparent: true, opacity: 0.9 });
    this.matGreenGlow = new THREE.MeshBasicMaterial({ color: 0x00ff66, blending: THREE.AdditiveBlending, transparent: true, opacity: 0.85 });
    this.matOrangeGlow = new THREE.MeshBasicMaterial({ color: 0xffa526, blending: THREE.AdditiveBlending, transparent: true, opacity: 0.9 });
    this.matRedGlow = new THREE.MeshBasicMaterial({ color: 0xff245f, blending: THREE.AdditiveBlending, transparent: true, opacity: 0.85 });
    this.matGoldGlow = new THREE.MeshBasicMaterial({ color: 0xffd76a, blending: THREE.AdditiveBlending, transparent: true, opacity: 0.9 });

    this.buildBase();
    this.buildObstacles();
    this.buildPads();
  }

  buildBase() {
    const floor = new THREE.Mesh(
      new THREE.CylinderGeometry(CONFIG.ARENA_RADIUS, CONFIG.ARENA_RADIUS, 0.5, 80),
      this.matFloor
    );
    floor.position.y = -0.25;
    this.scene.add(floor);
    this.obstacles.push(floor);

    const grid = new THREE.GridHelper(CONFIG.ARENA_RADIUS * 2, 48, 0x00eaff, 0x073060);
    grid.position.y = 0.015;
    this.scene.add(grid);

    const wall = new THREE.Mesh(
      new THREE.CylinderGeometry(CONFIG.ARENA_RADIUS, CONFIG.ARENA_RADIUS, CONFIG.ARENA_HEIGHT, 80, 1, true),
      this.matWall
    );
    wall.position.y = CONFIG.ARENA_HEIGHT / 2;
    this.scene.add(wall);

    const centerRing = new THREE.Mesh(new THREE.TorusGeometry(5.0, 0.04, 8, 96), this.matGoldGlow);
    centerRing.rotation.x = Math.PI / 2;
    centerRing.position.y = 0.06;
    this.scene.add(centerRing);

    for (let y = 3; y <= CONFIG.ARENA_HEIGHT; y += 3) {
      const ring = new THREE.Mesh(
        new THREE.TorusGeometry(CONFIG.ARENA_RADIUS - 0.1, y % 6 === 0 ? 0.05 : 0.02, 4, 112),
        y % 6 === 0 ? this.matCyanGlow : new THREE.MeshBasicMaterial({ color: 0x441188, blending: THREE.AdditiveBlending, transparent: true, opacity: 0.55 })
      );
      ring.rotation.x = Math.PI / 2;
      ring.position.y = y;
      this.scene.add(ring);
      this.animatables.push({ object: ring, speed: y % 6 === 0 ? 0.05 : -0.035 });
    }
  }

  addPlatform(x, y, z, sx, sz, accent = this.matCyanGlow) {
    const mesh = new THREE.Mesh(new THREE.BoxGeometry(sx, 0.42, sz), this.matObstacle);
    mesh.position.set(x, y, z);
    this.scene.add(mesh);
    this.obstacles.push(mesh);
    this.platforms.push({ y: y + 0.22, minX: x - sx / 2, maxX: x + sx / 2, minZ: z - sz / 2, maxZ: z + sz / 2 });

    const edges = new THREE.LineSegments(
      new THREE.EdgesGeometry(mesh.geometry),
      new THREE.LineBasicMaterial({ color: accent === this.matRedGlow ? 0xff245f : 0x00eaff, blending: THREE.AdditiveBlending })
    );
    edges.position.copy(mesh.position);
    this.scene.add(edges);
    return mesh;
  }

  addPillar(x, z, h, r, accent = this.matCyanGlow) {
    const mesh = new THREE.Mesh(new THREE.CylinderGeometry(r, r, h, 16), this.matObstacle);
    mesh.position.set(x, h / 2, z);
    this.scene.add(mesh);
    this.obstacles.push(mesh);

    for (const y of [1.1, h * 0.55, h - 0.55]) {
      const strip = new THREE.Mesh(new THREE.CylinderGeometry(r + 0.025, r + 0.025, 0.15, 16), accent);
      strip.position.set(x, y, z);
      this.scene.add(strip);
    }
  }

  buildObstacles() {
    this.addPlatform(0, 4.5, -6, 8, 4, this.matRedGlow);
    this.addPlatform(-9, 8.0, 3, 5, 4, this.matCyanGlow);
    this.addPlatform(8, 11.0, 4, 6, 4, this.matCyanGlow);
    this.addPlatform(0, 12.5, 0, 9, 3, this.matGoldGlow);

    this.addPlatform(-15, 6.4, -13.5, 6, 3, this.matRedGlow);
    this.addPlatform(15, 6.4, -13.5, 6, 3, this.matRedGlow);
    this.addPlatform(-15, 6.4, 13.5, 6, 3, this.matCyanGlow);
    this.addPlatform(15, 6.4, 13.5, 6, 3, this.matCyanGlow);

    this.addPillar(-10, -7, 10, 1.1, this.matRedGlow);
    this.addPillar(10, -8, 7, 1.3, this.matRedGlow);
    this.addPillar(7, 7, 12, 0.95, this.matCyanGlow);
    this.addPillar(-7, 9, 8.5, 1.05, this.matCyanGlow);
  }

  buildPads() {
    const addRecharge = (x, z) => {
      const g = new THREE.Group();
      g.position.set(x, 0.06, z);
      const pad = new THREE.Mesh(new THREE.CylinderGeometry(1.8, 1.8, 0.1, 32), this.matGreenGlow);
      const ring = new THREE.Mesh(new THREE.TorusGeometry(2.05, 0.045, 8, 48), this.matGreenGlow);
      ring.rotation.x = Math.PI / 2;
      g.add(pad, ring);
      this.scene.add(g);
      this.rechargePads.push({ group: g, x, z, radius: 2.15 });
    };
    addRecharge(-12, 0);
    addRecharge(12, 0);
    addRecharge(0, 13.5);

    const addLaunch = (x, z) => {
      const g = new THREE.Group();
      g.position.set(x, 0.08, z);
      const pad = new THREE.Mesh(new THREE.CylinderGeometry(1.35, 1.65, 0.16, 32), this.matOrangeGlow);
      const arrow = new THREE.Mesh(new THREE.ConeGeometry(0.38, 0.86, 24), this.matOrangeGlow);
      arrow.position.y = 0.68;
      g.add(pad, arrow);
      this.scene.add(g);
      this.launchPads.push({ group: g, x, z, radius: 1.75, boost: 20.5, cooldown: 0 });
    };
    addLaunch(0, 12);
    addLaunch(0, -12);
    addLaunch(-17, 13);
    addLaunch(17, 13);
  }

  getFloorY(playerPos) {
    let groundY = 0;
    for (const plat of this.platforms) {
      if (playerPos.x > plat.minX && playerPos.x < plat.maxX && playerPos.z > plat.minZ && playerPos.z < plat.maxZ) {
        if (playerPos.y >= plat.y - 0.35 && playerPos.y < plat.y + 1.55) groundY = Math.max(groundY, plat.y);
      }
    }
    return groundY;
  }

  update(dt) {
    for (const item of this.animatables) item.object.rotation.z += item.speed * dt;
    for (const pad of this.rechargePads) pad.group.rotation.y += dt * 1.4;
    for (const pad of this.launchPads) {
      pad.group.rotation.y += dt * 2.2;
      pad.cooldown = Math.max(0, pad.cooldown - dt);
    }
  }
}
