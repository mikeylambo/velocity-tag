import * as THREE from 'three';
import { CONFIG } from './config.js';
import { EventBus } from './eventBus.js';

export class TargetManager {
  constructor(scene, arena) {
    this.scene = scene;
    this.arena = arena;
    this.targets = [];
    this.hitZones = [];
    
    // PRE-ALLOCATED STABILITY: Centralized standard assets preventing allocation stutter
    this.matBody = new THREE.MeshStandardMaterial({ color: 0x101015, roughness: 0.4 });
    this.matRedVisor = new THREE.MeshBasicMaterial({ color: 0xff245f });
    this.matBlueVisor = new THREE.MeshBasicMaterial({ color: 0x00eaff });
    this.matHead = new THREE.MeshBasicMaterial({ color: 0xffd76a });
    this.matRedBack = new THREE.MeshBasicMaterial({ color: 0xff245f });
    this.matBlueBack = new THREE.MeshBasicMaterial({ color: 0x00eaff });

    // IMPACT REACTION ASSETS: Satisfying feedback flashes mapping straight across body zones
    this.matHitChest = new THREE.MeshBasicMaterial({ color: 0xff66cc });
    this.matHitHead = new THREE.MeshBasicMaterial({ color: 0xffffff });
    this.matHitBack = new THREE.MeshBasicMaterial({ color: 0xbf55ec });
    
    this.matGhost = new THREE.MeshBasicMaterial({ color: 0x00eaff, wireframe: true, transparent: true, opacity: 0.4 });

    this.init();
    this.bind();
  }

  clearTargets() {
    this.targets.forEach(t => {
      t.group.removeFromParent();
      Object.values(t.meshes).forEach(m => {
        if (m.geometry) m.geometry.dispose();
      });
    });
    this.targets = [];
    this.hitZones = [];
  }

  init() {
    this.clearTargets();
    const spawns = (this.arena && this.arena.mapData && this.arena.mapData.targetSpawns) 
      ? this.arena.mapData.targetSpawns 
      : [
          { x: -14, y: 4.2, z: 0 }, { x: 14, y: 4.2, z: 0 },
          { x: 0, y: 8.2, z: -14 }, { x: -8, y: 1.2, z: -8 },
          { x: 8, y: 1.2, z: -8 }, { x: 0, y: 1.2, z: 8 }
        ];
    
    spawns.forEach((pos, i) => {
      const g = new THREE.Group(); 
      g.position.set(pos.x, pos.y, pos.z);
      this.scene.add(g);

      const assignedTeam = i % 2 === 0 ? 'RED' : 'BLUE';

      const t = { 
        group: g, 
        team: assignedTeam,
        state: 'PATROL', 
        charges: 1, // Enforce strict 1-charge rules matching Feel Pass criteria
        maxCharges: 1,
        isGhosted: false, 
        ghostTimer: 0, 
        velocity: new THREE.Vector3(), 
        spinVel: new THREE.Vector3(), 
        speed: 2.0 + Math.random() * 1.5,
        meshes: {}
      };

      const angle = (i / spawns.length) * Math.PI * 2;
      t.velocity.set(Math.cos(angle), 0, Math.sin(angle)).multiplyScalar(t.speed);

      const body = new THREE.Mesh(new THREE.BoxGeometry(0.55, 0.75, 0.35), this.matBody);
      body.position.y = 1.0; g.add(body);
      
      const activeVisorMat = assignedTeam === 'RED' ? this.matRedVisor : this.matBlueVisor;
      
      const chest = new THREE.Mesh(new THREE.BoxGeometry(0.45, 0.3, 0.1), activeVisorMat);
      chest.position.set(0, 1.1, -0.18); g.add(chest);
      chest.userData = { type: 'target', tagType: 'CHEST', score: 100, parentTarget: t };
      this.hitZones.push(chest);

      const head = new THREE.Mesh(new THREE.BoxGeometry(0.35, 0.1, 0.1), this.matHead);
      head.position.set(0, 1.6, -0.18); g.add(head);
      head.userData = { type: 'target', tagType: 'HELMET', score: 150, parentTarget: t };
      this.hitZones.push(head);

      const backMat = assignedTeam === 'RED' ? this.matRedBack : this.matBlueBack;
      const back = new THREE.Mesh(new THREE.BoxGeometry(0.3, 0.4, 0.12), backMat);
      back.position.set(0, 1.1, 0.2); g.add(back);
      back.userData = { type: 'target', tagType: 'FLANK_PACK', score: 250, parentTarget: t };
      this.hitZones.push(back);

      t.meshes = { body, chest, head, back };
      this.targets.push(t);
    });
  }

  bind() {
    EventBus.on('TARGET_HIT_CONFIRMED', ({ target }) => {
      if (target.isGhosted) return;
      
      target.charges--;
      target.isGhosted = true;
      target.ghostTimer = CONFIG.GHOST_DURATION || 0.75; // Shorter 0.75s ghost recovery window
      
      // Visual feedback layering: Flash zones instantly before settling into ghost shimmers
      target.meshes.chest.material = this.matHitChest;
      target.meshes.head.material = this.matHitHead;
      target.meshes.back.material = this.matHitBack;
      
      // Apply immediate physical impact recoil spin
      target.spinVel.set((Math.random() - 0.5) * 8, (Math.random() - 0.5) * 8, 0);

      // Transition smoothly to uniform ghost transparency after initial strike display
      setTimeout(() => {
        if (target.isGhosted && target.group.parent) {
          Object.values(target.meshes).forEach(m => m.material = this.matGhost);
        }
      }, 80);
    });

    EventBus.on('TIME_ATTACK_START', () => {
      this.init();
    });
  }

  resetTarget(t, avatarPos = null) {
    t.isGhosted = false; 
    t.charges = t.maxCharges; 
    t.state = 'PATROL';
    t.group.rotation.set(0, 0, 0); 
    t.spinVel.set(0, 0, 0);
    
    // Safely restore cached team colorations without garbage generation
    t.meshes.body.material = this.matBody; 
    t.meshes.chest.material = t.team === 'RED' ? this.matRedVisor : this.matBlueVisor; 
    t.meshes.head.material = this.matHead;
    t.meshes.back.material = t.team === 'RED' ? this.matRedBack : this.matBlueBack;

    // Reposition immediately straight to a distant route point
    const spawns = (this.arena && this.arena.mapData && this.arena.mapData.targetSpawns) 
      ? this.arena.mapData.targetSpawns 
      : [{ x: -14, y: 4.2, z: 0 }, { x: 14, y: 4.2, z: 0 }, { x: 0, y: 8.2, z: -14 }];
    
    let validSpawns = spawns;
    if (avatarPos) {
      const distantSpawns = spawns.filter(s => Math.hypot(s.x - avatarPos.x, s.z - avatarPos.z) >= CONFIG.MIN_TARGET_RESPAWN_DISTANCE_FROM_PLAYER);
      if (distantSpawns.length > 0) validSpawns = distantSpawns;
    }
    
    const s = validSpawns[Math.floor(Math.random() * validSpawns.length)];
    t.group.position.set(s.x, s.y, s.z);
    const angle = Math.atan2(-s.z, -s.x);
    t.velocity.set(Math.cos(angle), 0, Math.sin(angle)).multiplyScalar(t.speed);
  }

  update(avatarPos, isPlaying, dt) {
    this.targets.forEach(t => {
      const g = t.group;
      const floorY = this.arena ? this.arena.getFloorY(g.position) : 0;

      if (g.position.y < floorY + 1.0) {
        g.position.y = floorY + 1.0;
        if (t.velocity.y < 0) t.velocity.y = 0;
      }

      if (this.arena && this.arena.resolveCollisions) {
        this.arena.resolveCollisions(g.position, null, 0.4);
      }

      if (t.isGhosted) {
        t.ghostTimer -= dt;
        g.rotation.x += t.spinVel.x * dt;
        g.rotation.y += t.spinVel.y * dt;
        
        if (t.ghostTimer <= 0) {
          // Instantly recycle and reposition to keep routing density fully locked
          this.resetTarget(t, avatarPos);
        }
        return;
      }

      if (isPlaying) {
        g.position.addScaledVector(t.velocity, dt);
        const d = Math.hypot(g.position.x, g.position.z);
        if (d > CONFIG.ARENA_RADIUS - 2) { 
          t.velocity.x *= -1; 
          t.velocity.z *= -1; 
        }
        
        // Gentle operational rotation tracking movement trajectory vectors
        const rot = Math.atan2(-t.velocity.x, -t.velocity.z);
        g.rotation.y = THREE.MathUtils.lerp(g.rotation.y, rot, dt * 5);
      }
    });
  }
}