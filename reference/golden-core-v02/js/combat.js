import * as THREE from 'three';
import { CONFIG } from './config.js';

export class CombatManager {
  constructor(raycaster) {
    this.raycaster = raycaster;
    this.lastFireTime = -99;
  }

  reset() {
    this.lastFireTime = -99;
  }

  processFire(inputState, cameraMgr, player, arena, targetMgr, roundMgr, fx, now) {
    if (!inputState.firePressed || roundMgr.state.phase !== 'playing') return;
    if (now - this.lastFireTime < CONFIG.FIRE_COOLDOWN) return;

    this.lastFireTime = now;
    roundMgr.state.shots++;

    const aimRay = cameraMgr.getAimRay(player.facingAngle);
    this.raycaster.set(aimRay.origin, aimRay.direction);
    this.raycaster.far = CONFIG.LASER_RANGE;

    const liveHitZones = targetMgr.hitZones.filter(z => z.visible && z.userData.parentTarget.active);
    const hits = this.raycaster.intersectObjects([...liveHitZones, ...arena.obstacles], true);

    let end = aimRay.origin.clone().addScaledVector(aimRay.direction, CONFIG.LASER_RANGE);
    let hitZone = null;

    if (hits.length) {
      end = hits[0].point.clone();
      let obj = hits[0].object;
      while (obj && obj.userData && obj.userData.type !== 'target' && obj.parent) obj = obj.parent;
      if (obj && obj.userData && obj.userData.type === 'target') hitZone = obj;
    }

    // The ray is camera/controller based; the beam starts from the avatar gun for sports readability.
    const start = player.getGunWorldPosition();
    fx.spawnLaserBeam(start, end, hitZone ? 0xffd76a : 0x00eaff);

    if (hitZone) {
      const tgtData = hitZone.userData;
      const airborneBonus = player.isGrounded ? 0 : 50;
      const score = tgtData.score + airborneBonus;

      roundMgr.addScore(score, tgtData.tagType);
      targetMgr.disableTarget(tgtData.parentTarget);

      fx.spawnHitBurst(end, tgtData.tagType === 'HELMET' ? 0xffd76a : 0xff245f);
      fx.spawnFloatingText(`+${score} ${tgtData.tagType}`, end.clone().add(new THREE.Vector3(0, 0.65, 0)));
    }
  }
}
