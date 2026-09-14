import * as THREE from 'three';
import { CONFIG } from './config.js';
import { EventBus } from './eventBus.js';

export class CombatManager {
  constructor(raycaster, targetManager, cameraManager) {
    this.raycaster = raycaster;
    this.targetManager = targetManager;
    this.cameraManager = cameraManager;
    
    // Strict rhythm pacing logic
    this.fireCooldown = 0;
    this.isPlaying = false;
    this.isAirborne = false;
    this.playerPosition = new THREE.Vector3();
    
    // Contextual Route & Drop Bonus Tracking
    this.lastLaunchTime = -999;
    this.lastQuickDropTime = -999;
    this.lastQuickDropLandTime = -999;
    this.wasAirborneLastFrame = false;

    this.bindEvents();
  }

  bindEvents() {
    // Firing executes cleanly directly through the sport-like cooldown constraints
    EventBus.on('INPUT_FIRE', () => this.executeAimFire());
    
    // Listen natively for Quick Drop activation to arm the contextual scoring timer
    EventBus.on('EXECUTE_QUICK_DROP', () => {
      if (this.isPlaying && this.isAirborne) {
        this.lastQuickDropTime = performance.now() / 1000;
      }
    });

    EventBus.on('PLAYER_TELEMETRY', ({ position, isGrounded }) => {
      this.isAirborne = !isGrounded;
      this.playerPosition.copy(position);

      // Detect exact landing frame following an airborne Quick Drop sequence
      if (this.wasAirborneLastFrame && isGrounded) {
        const now = performance.now() / 1000;
        // If landing occurs within 2 seconds of triggering a quick drop, arm the 1.5s reward window
        if (now - this.lastQuickDropTime <= 2.0) {
          this.lastQuickDropLandTime = now;
          EventBus.emit('LOG_DEBUG', { message: 'QUICK DROP LANDING: Bonus Armed (1.5s)' });
        }
      }
      this.wasAirborneLastFrame = this.isAirborne;
    });

    EventBus.on('ROUND_STATE_CHANGED', ({ phase }) => {
      this.isPlaying = phase === 'playing';
    });
    
    EventBus.on('TIME_ATTACK_START', () => {
      this.fireCooldown = 0;
      this.isPlaying = false;
      this.lastLaunchTime = -999;
      this.lastQuickDropTime = -999;
      this.lastQuickDropLandTime = -999;
    });

    // Capture Launch Route engagement natively
    EventBus.on('LOG_DEBUG', ({ message }) => {
      if (message && message.includes('LAUNCH ROUTE ENGAGED')) {
        this.lastLaunchTime = performance.now() / 1000;
      }
    });

    // Handle incoming hostile payloads cleanly
    EventBus.on('HOSTILE_FIRE_INCOMING', ({ origin, targetAim }) => {
      EventBus.emit('SPAWN_BEAM', { start: origin, end: targetAim, color: 0xff245f });
      const avatarCenter = this.playerPosition.clone().add(new THREE.Vector3(0, 1.0, 0));
      if (targetAim.distanceTo(avatarCenter) < 1.2) {
        EventBus.emit('PLAYER_HIT', { damage: 50 });
      }
    });
  }

  executeAimFire() {
    // Validate operational status strictly against active match constraints and exact 1.0s shot clock
    if (!this.isPlaying || this.fireCooldown > 0) return;
    
    this.fireCooldown = CONFIG.PLAYER_FIRE_COOLDOWN || 1.0;

    const aimRay = this.cameraManager.getAimRay(this.cameraManager.rig.rotation.y);
    this.raycaster.set(aimRay.origin, aimRay.direction);
    this.raycaster.far = CONFIG.LASER_RANGE;

    let liveHitZones = [];
    if (this.targetManager && this.targetManager.hitZones) {
      liveHitZones = this.targetManager.hitZones.filter(z => {
        const parent = z.userData.parentTarget;
        return parent && !parent.isGhosted && !parent.isDepleted;
      });
    }

    const hits = this.raycaster.intersectObjects(liveHitZones, true);
    let end = aimRay.origin.clone().addScaledVector(aimRay.direction, CONFIG.LASER_RANGE);
    let hitZone = null;

    if (hits.length > 0) {
      end = hits[0].point.clone();
      hitZone = hits[0].object;
    }

    // Explicitly broadcast validated outbound discharge to register accuracy metrics
    EventBus.emit('WEAPON_FIRED');
    EventBus.emit('SPAWN_BEAM', { start: aimRay.origin, end, color: hitZone ? 0xffd76a : 0x00eaff });

    if (hitZone) {
      const tgtData = hitZone.userData;
      const now = performance.now() / 1000;
      
      // Evaluate contextual timing windows
      const isLaunchBonus = (now - this.lastLaunchTime) <= 2.0;
      const isQuickDropBonus = (now - this.lastQuickDropLandTime) <= 1.5;

      // Report contact parameters up to the single scoring authority
      EventBus.emit('TARGET_HIT_REPORT', { 
        target: tgtData.parentTarget, 
        tagType: tgtData.tagType,
        isAirborne: this.isAirborne,
        isLaunchBonus: isLaunchBonus,
        isQuickDropBonus: isQuickDropBonus,
        impactPoint: end
      });
    }
  }

  update(dt) {
    if (this.fireCooldown > 0) {
      this.fireCooldown -= dt;
    }

    // Dynamic Reticle Feedback: Bright green/cyan when charged, crimson while reloading
    if (this.cameraManager && this.cameraManager.aimReticle && this.cameraManager.aimReticle.material) {
      const isReady = this.isPlaying && this.fireCooldown <= 0;
      const targetHex = isReady ? 0x00ff66 : 0xff245f;
      this.cameraManager.aimReticle.material.color.setHex(targetHex);
    }
  }
}