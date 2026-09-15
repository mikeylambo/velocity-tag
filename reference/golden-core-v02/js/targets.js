import * as THREE from 'three';
import { CONFIG } from './config.js';

export class TargetManager {
  constructor(scene) {
    this.scene = scene;
    this.targets = [];
    this.hitZones = [];

    this.matBase = new THREE.MeshStandardMaterial({ color: 0x151a25, roughness: 0.45, metalness: 0.45 });
    this.matChest = new THREE.MeshBasicMaterial({ color: 0xff245f, blending: THREE.AdditiveBlending });
    this.matHead = new THREE.MeshBasicMaterial({ color: 0xffd76a, blending: THREE.AdditiveBlending });
    this.matRing = new THREE.MeshBasicMaterial({ color: 0x8b5dff, blending: THREE.AdditiveBlending, transparent: true, opacity: 0.9 });

    this.spawnPads = [
      new THREE.Vector3(-12, 0, -10),
      new THREE.Vector3(12, 0, -10),
      new THREE.Vector3(0, 0, -14),
      new THREE.Vector3(-6, 4.9, -6),
      new THREE.Vector3(6, 4.9, -6),
      new THREE.Vector3(-13.5, 6.8, 13.5),
      new THREE.Vector3(13.5, 6.8, 13.5),
      new THREE.Vector3(0, 12.8, 0)
    ];

    this.initTargets();
  }

  initTargets() {
    for (let i = 0; i < this.spawnPads.length; i++) {
      const g = new THREE.Group();
      g.position.copy(this.spawnPads[i]);
      this.scene.add(g);

      const tgt = {
        group: g,
        active: true,
        respawnTimer: 0,
        basePos: this.spawnPads[i].clone(),
        bobSeed: Math.random() * Math.PI * 2,
        hitZones: []
      };

      const body = new THREE.Mesh(new THREE.BoxGeometry(0.62, 0.82, 0.42), this.matBase);
      body.position.y = 1.0;
      g.add(body);

      const chest = new THREE.Mesh(new THREE.BoxGeometry(0.47, 0.34, 0.09), this.matChest);
      chest.position.set(0, 1.1, -0.24);
      chest.userData = { type: 'target', tagType: 'CHEST', score: 100, parentTarget: tgt };
      g.add(chest);
      this.hitZones.push(chest);
      tgt.hitZones.push(chest);

      const head = new THREE.Mesh(new THREE.BoxGeometry(0.38, 0.38, 0.38), this.matBase);
      head.position.y = 1.6;
      g.add(head);

      const helmet = new THREE.Mesh(new THREE.BoxGeometry(0.42, 0.14, 0.09), this.matHead);
      helmet.position.set(0, 1.67, -0.24);
      helmet.userData = { type: 'target', tagType: 'HELMET', score: 150, parentTarget: tgt };
      g.add(helmet);
      this.hitZones.push(helmet);
      tgt.hitZones.push(helmet);

      const ring = new THREE.Mesh(new THREE.TorusGeometry(0.72, 0.025, 8, 32), this.matRing);
      ring.rotation.x = Math.PI / 2;
      ring.position.y = 0.05;
      g.add(ring);
      tgt.ring = ring;

      this.targets.push(tgt);
    }
  }

  resetAll() {
    for (const tgt of this.targets) {
      tgt.active = true;
      tgt.respawnTimer = 0;
      tgt.group.visible = true;
      tgt.group.position.copy(tgt.basePos);
      tgt.hitZones.forEach(z => z.visible = true);
    }
  }

  disableTarget(tgt) {
    tgt.active = false;
    tgt.respawnTimer = CONFIG.TARGET_RESPAWN;
    tgt.group.visible = false;
  }

  update(avatarPos, dt, elapsed = 0) {
    for (const tgt of this.targets) {
      if (!tgt.active) {
        tgt.respawnTimer -= dt;
        if (tgt.respawnTimer <= 0) {
          tgt.active = true;
          tgt.group.visible = true;
          tgt.hitZones.forEach(z => z.visible = true);
        }
        continue;
      }

      tgt.group.position.y = tgt.basePos.y + Math.sin(elapsed * 1.35 + tgt.bobSeed) * 0.10;
      tgt.ring.rotation.z += dt * 1.8;

      const lookAt = avatarPos.clone();
      lookAt.y = tgt.group.position.y + 1.05;
      tgt.group.lookAt(lookAt);

      tgt.hitZones.forEach((z, idx) => {
        z.scale.setScalar(1 + Math.sin(elapsed * 5.5 + idx + tgt.bobSeed) * 0.025);
      });
    }
  }
}
