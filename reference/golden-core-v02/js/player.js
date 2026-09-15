import * as THREE from 'three';
import { CONFIG } from './config.js';

export class PlayerAvatar {
  constructor(scene) {
    this.group = new THREE.Group();
    this.group.position.set(0, 0, 9);
    scene.add(this.group);

    this.velocity = new THREE.Vector3();
    this.dashVelocity = new THREE.Vector3();
    this.dashTimer = 0;
    this.facingAngle = 0;
    this.isGrounded = true;
    this.onRechargePad = false;
    this.fuel = 100;
    this.runCycle = 0;

    this.buildMesh();
  }

  buildMesh() {
    const matBody = new THREE.MeshStandardMaterial({ color: 0x182033, emissive: 0x030816, roughness: 0.25, metalness: 0.6 });
    const matCyan = new THREE.MeshBasicMaterial({ color: 0x00eaff, blending: THREE.AdditiveBlending, transparent: true, opacity: 0.95 });
    const matGold = new THREE.MeshBasicMaterial({ color: 0xffd76a, blending: THREE.AdditiveBlending, transparent: true, opacity: 0.95 });

    this.torso = new THREE.Mesh(new THREE.BoxGeometry(0.65, 0.86, 0.42), matBody);
    this.torso.position.y = 1.05;
    this.group.add(this.torso);

    const chest = new THREE.Mesh(new THREE.BoxGeometry(0.42, 0.30, 0.07), matCyan);
    chest.position.set(0, 1.13, -0.24);
    this.group.add(chest);

    const head = new THREE.Mesh(new THREE.BoxGeometry(0.36, 0.36, 0.36), matBody);
    head.position.y = 1.62;
    this.group.add(head);

    const visor = new THREE.Mesh(new THREE.BoxGeometry(0.38, 0.09, 0.06), matGold);
    visor.position.set(0, 1.64, -0.22);
    this.group.add(visor);

    this.legL = new THREE.Mesh(new THREE.BoxGeometry(0.20, 0.70, 0.22), matBody);
    this.legL.position.set(-0.17, 0.40, 0);
    this.legR = this.legL.clone();
    this.legR.position.x = 0.17;
    this.group.add(this.legL, this.legR);

    const pack = new THREE.Mesh(new THREE.BoxGeometry(0.52, 0.62, 0.25), matBody);
    pack.position.set(0, 1.0, 0.32);
    this.group.add(pack);

    this.gunBarrel = new THREE.Mesh(new THREE.CylinderGeometry(0.035, 0.035, 0.45, 10), matCyan);
    this.gunBarrel.rotation.x = Math.PI / 2;
    this.gunBarrel.position.set(0.45, 1.05, -0.25);
    this.group.add(this.gunBarrel);

    this.marker = new THREE.Group();
    this.groundRing = new THREE.Mesh(new THREE.TorusGeometry(0.92, 0.035, 8, 48), matCyan);
    this.groundRing.rotation.x = Math.PI / 2;
    this.groundRing.position.y = 0.04;
    this.beacon = new THREE.Mesh(new THREE.ConeGeometry(0.16, 0.42, 16), matCyan);
    this.beacon.position.set(0, 2.35, 0);
    this.beacon.rotation.x = Math.PI;
    this.marker.add(this.groundRing, this.beacon);
    this.group.add(this.marker);
  }

  reset() {
    this.group.position.set(0, 0, 9);
    this.velocity.set(0, 0, 0);
    this.dashVelocity.set(0, 0, 0);
    this.dashTimer = 0;
    this.facingAngle = 0;
    this.group.rotation.y = 0;
    this.isGrounded = true;
    this.onRechargePad = false;
    this.fuel = 100;
  }

  getGunWorldPosition() {
    const pos = new THREE.Vector3();
    this.gunBarrel.getWorldPosition(pos);
    return pos;
  }

  update(inputState, arena, fx, dt) {
    if (Math.abs(inputState.turn) > 0.05) {
      this.facingAngle -= inputState.turn * CONFIG.TURN_SPEED * dt;
    }
    this.group.rotation.y = this.facingAngle;

    const up = new THREE.Vector3(0, 1, 0);
    const forwardVec = new THREE.Vector3(0, 0, -1).applyAxisAngle(up, this.facingAngle);
    const rightVec = new THREE.Vector3(1, 0, 0).applyAxisAngle(up, this.facingAngle);

    const desiredPlanar = new THREE.Vector3()
      .addScaledVector(forwardVec, -inputState.planar.z)
      .addScaledVector(rightVec, inputState.planar.x);
    if (desiredPlanar.lengthSq() > 1) desiredPlanar.normalize();

    const groundY = arena.getFloorY(this.group.position);

    this.onRechargePad = false;
    for (const pad of arena.rechargePads) {
      if (Math.hypot(this.group.position.x - pad.x, this.group.position.z - pad.z) < pad.radius && this.group.position.y <= 0.2) {
        this.onRechargePad = true;
        break;
      }
    }

    for (const pad of arena.launchPads) {
      if (pad.cooldown <= 0 && Math.hypot(this.group.position.x - pad.x, this.group.position.z - pad.z) < pad.radius && this.group.position.y <= 0.3) {
        this.velocity.y = Math.max(this.velocity.y, pad.boost);
        this.isGrounded = false;
        pad.cooldown = 1.15;
        this.fuel = Math.min(100, this.fuel + 10);
        fx.spawnPadBoost(new THREE.Vector3(pad.x, 0.4, pad.z));
        break;
      }
    }

    if (inputState.dashPressed && this.fuel >= CONFIG.DASH_COST && this.dashTimer <= 0) {
      const dashDir = desiredPlanar.lengthSq() > 0.02 ? desiredPlanar.clone().normalize() : forwardVec.clone();
      this.dashVelocity.copy(dashDir).multiplyScalar(CONFIG.DASH_SPEED);
      this.dashTimer = CONFIG.DASH_DURATION;
      this.fuel = Math.max(0, this.fuel - CONFIG.DASH_COST);
      fx.spawnJetExhaust(this);
    }

    if (inputState.dropPressed && !this.isGrounded) {
      this.velocity.y = Math.min(this.velocity.y, -CONFIG.QUICK_DROP_SPEED);
    }

    const isJetting = inputState.thrust > 0.05 && this.fuel > 0;
    if (isJetting) {
      this.velocity.y += inputState.thrust * CONFIG.VERTICAL_THRUST * dt;
      this.velocity.y = Math.min(this.velocity.y, CONFIG.VERTICAL_MAX);
      this.fuel = Math.max(0, this.fuel - 28 * dt);
      fx.spawnJetExhaust(this);
    } else if (inputState.thrust < -0.05) {
      this.velocity.y = Math.max(this.velocity.y - CONFIG.DESCEND_SPEED * dt, -CONFIG.VERTICAL_MAX);
    } else {
      this.velocity.y -= CONFIG.GRAVITY * dt;
    }

    const moveSpeed = this.isGrounded ? CONFIG.MOVE_SPEED : CONFIG.AIR_MOVE_SPEED;
    if (this.dashTimer > 0) {
      this.dashTimer -= dt;
      this.velocity.x = this.dashVelocity.x;
      this.velocity.z = this.dashVelocity.z;
    } else {
      this.velocity.x = desiredPlanar.x * moveSpeed;
      this.velocity.z = desiredPlanar.z * moveSpeed;
    }

    this.group.position.addScaledVector(this.velocity, dt);

    this.isGrounded = false;
    if (this.group.position.y <= groundY) {
      this.group.position.y = groundY;
      if (this.velocity.y < 0) this.velocity.y = 0;
      this.isGrounded = true;
    }

    if (!isJetting) {
      const rate = this.onRechargePad ? CONFIG.FUEL_REGEN_PAD : (this.isGrounded ? CONFIG.FUEL_REGEN_GROUND : CONFIG.FUEL_REGEN_AIR);
      this.fuel = Math.min(100, this.fuel + rate * dt);
    }

    if (Math.hypot(this.group.position.x, this.group.position.z) > CONFIG.ARENA_RADIUS - CONFIG.PLAYER_RADIUS) {
      const angle = Math.atan2(this.group.position.z, this.group.position.x);
      this.group.position.x = Math.cos(angle) * (CONFIG.ARENA_RADIUS - CONFIG.PLAYER_RADIUS);
      this.group.position.z = Math.sin(angle) * (CONFIG.ARENA_RADIUS - CONFIG.PLAYER_RADIUS);
    }
    this.group.position.y = THREE.MathUtils.clamp(this.group.position.y, 0, CONFIG.ARENA_HEIGHT - 1.3);

    if (this.isGrounded && desiredPlanar.lengthSq() > 0.05) {
      this.runCycle += dt * 15;
      this.torso.position.y = 1.05 + Math.sin(this.runCycle) * 0.06;
      this.legL.rotation.x = Math.sin(this.runCycle) * 0.34;
      this.legR.rotation.x = -Math.sin(this.runCycle) * 0.34;
    } else {
      this.torso.position.y = THREE.MathUtils.lerp(this.torso.position.y, 1.05, dt * 6);
      this.legL.rotation.x = THREE.MathUtils.lerp(this.legL.rotation.x, 0, dt * 7);
      this.legR.rotation.x = THREE.MathUtils.lerp(this.legR.rotation.x, 0, dt * 7);
    }

    this.groundRing.rotation.z += dt * 1.8;
    this.beacon.position.y = 2.35 + Math.sin(performance.now() * 0.006) * 0.06;
  }
}
