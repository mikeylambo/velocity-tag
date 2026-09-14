import * as THREE from 'three';
import { CONFIG } from './config.js';
import { EventBus } from './eventBus.js';

export class PlayerAvatar {
  constructor(scene) {
    this.group = new THREE.Group();
    scene.add(this.group);

    this.velocity = new THREE.Vector3();
    this.dashVelocity = new THREE.Vector3();
    this.dashTimer = 0;
    this.facingAngle = 0;
    this.isGrounded = true;
    this.fuel = 100;
    this.hitTimer = 0;

    this.planarInput = { x: 0, z: 0 };
    
    this.matMech = new THREE.MeshStandardMaterial({ color: 0x111115, roughness: 0.2 });
    this.matDamage = new THREE.MeshBasicMaterial({ color: 0xff245f });

    this.buildMesh();
    this.bindEvents();
  }

  buildMesh() {
    const matCyan = new THREE.MeshBasicMaterial({ color: 0x00eaff });
    
    this.chassis = new THREE.Mesh(new THREE.BoxGeometry(0.6, 0.8, 0.4), this.matMech);
    this.chassis.position.y = 1.0;
    this.group.add(this.chassis);

    const visor = new THREE.Mesh(new THREE.BoxGeometry(0.35, 0.1, 0.15), matCyan);
    visor.position.set(0, 1.45, -0.18);
    this.group.add(visor);

    this.blaster = new THREE.Mesh(new THREE.CylinderGeometry(0.04, 0.04, 0.4, 8), matCyan);
    this.blaster.rotation.x = Math.PI / 2;
    this.blaster.position.set(0.4, 1.0, -0.2);
    this.group.add(this.blaster);
  }

  bindEvents() {
    EventBus.on('INPUT_DASH', () => {
      if (this.fuel >= 20 && this.dashTimer <= 0) {
        const forward = new THREE.Vector3(0, 0, -1).applyAxisAngle(new THREE.Vector3(0, 1, 0), this.facingAngle);
        const right = new THREE.Vector3(1, 0, 0).applyAxisAngle(new THREE.Vector3(0, 1, 0), this.facingAngle);
        
        const dir = new THREE.Vector3();
        if (Math.abs(this.planarInput.x) > 0.1 || Math.abs(this.planarInput.z) > 0.1) {
          dir.addScaledVector(forward, this.planarInput.z);
          dir.addScaledVector(right, this.planarInput.x);
        } else {
          dir.copy(forward);
        }
        
        if (dir.lengthSq() > 0) dir.normalize();
        this.dashVelocity.copy(dir).multiplyScalar(CONFIG.DASH_SPEED || 18.0);
        this.dashTimer = 0.18;
        this.fuel -= 20;
        EventBus.emit('SPAWN_EXHAUST', { position: this.group.position });
      }
    });

    // QUICK DROP EXECUTION: Immediately nullifies lift and forces sharp downward descent
    EventBus.on('EXECUTE_QUICK_DROP', () => {
      if (!this.isGrounded) {
        this.velocity.y = -(CONFIG.DASH_SPEED || 18.0) * 1.5;
      }
    });

    EventBus.on('PLAYER_HIT', () => {
      if (this.hitTimer <= 0) {
        this.hitTimer = 0.35;
        this.chassis.material = this.matDamage;
        EventBus.emit('LOG_DEBUG', { message: 'CHASSIS DAMAGE DETECTED' });
      }
    });

    EventBus.on('ROUND_START', () => {
      this.group.position.set(0, 0, 12);
      this.velocity.set(0, 0, 0);
      this.fuel = 100;
      this.hitTimer = 0;
      this.chassis.material = this.matMech;
    });
  }

  update(inputState, arena, dt) {
    this.planarInput.x = inputState.planar.x;
    this.planarInput.z = inputState.planar.z;

    if (this.hitTimer > 0) {
      this.hitTimer -= dt;
      if (this.hitTimer <= 0) this.chassis.material = this.matMech;
    }

    if (Math.abs(inputState.turn) > 0.05) {
      this.facingAngle -= inputState.turn * CONFIG.TURN_SPEED * dt;
    }
    this.group.rotation.y = this.facingAngle;

    const forward = new THREE.Vector3(0, 0, -1).applyAxisAngle(new THREE.Vector3(0, 1, 0), this.facingAngle);
    const right = new THREE.Vector3(1, 0, 0).applyAxisAngle(new THREE.Vector3(0, 1, 0), this.facingAngle);

    const moveDir = new THREE.Vector3()
      .addScaledVector(forward, inputState.planar.z)
      .addScaledVector(right, inputState.planar.x);
    if (moveDir.lengthSq() > 1) moveDir.normalize();

    const floorY = arena ? arena.getFloorY(this.group.position) : 0;
    const thrusting = inputState.thrust > 0.1 && this.fuel > 0;

    if (thrusting) {
      this.velocity.y += inputState.thrust * CONFIG.VERTICAL_THRUST * dt;
      this.velocity.y = Math.min(this.velocity.y, CONFIG.VERTICAL_MAX);
      this.fuel -= 25 * dt;
      EventBus.emit('SPAWN_EXHAUST', { position: this.group.position });
    } else {
      this.velocity.y -= CONFIG.GRAVITY * dt;
    }

    if (this.dashTimer > 0) {
      this.dashTimer -= dt;
      this.velocity.x = this.dashVelocity.x;
      this.velocity.z = this.dashVelocity.z;
    } else {
      const speed = this.isGrounded ? CONFIG.MOVE_SPEED : CONFIG.AIR_MOVE_SPEED;
      this.velocity.x = moveDir.x * speed;
      this.velocity.z = moveDir.z * speed;
    }

    this.group.position.addScaledVector(this.velocity, dt);

    this.isGrounded = false;
    if (this.group.position.y <= floorY) {
      this.group.position.y = floorY;
      if (this.velocity.y < 0) this.velocity.y = 0;
      this.isGrounded = true;
    }

    if (!thrusting) {
      this.fuel = Math.min(100, this.fuel + (this.isGrounded ? 40 : 12) * dt);
    }

    const dist = Math.hypot(this.group.position.x, this.group.position.z);
    if (dist > CONFIG.ARENA_RADIUS - 1.0) {
      const angle = Math.atan2(this.group.position.z, this.group.position.x);
      this.group.position.x = Math.cos(angle) * (CONFIG.ARENA_RADIUS - 1.0);
      this.group.position.z = Math.sin(angle) * (CONFIG.ARENA_RADIUS - 1.0);
    }

    EventBus.emit('PLAYER_TELEMETRY', { position: this.group.position, fuel: this.fuel, isGrounded: this.isGrounded });
  }
}