import * as THREE from 'three';
import { CONFIG } from './config.js';

export class CameraManager {
  constructor(scene, renderer) {
    this.scene = scene;
    this.renderer = renderer;

    this.rig = new THREE.Group();
    this.camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.05, 1000);
    this.rig.add(this.camera);
    scene.add(this.rig);

    this.controllers = [];
    this.tmpMatrix = new THREE.Matrix4();

    this.aimReticle = new THREE.Group();
    const core = new THREE.Mesh(
      new THREE.SphereGeometry(0.075, 12, 8),
      new THREE.MeshBasicMaterial({ color: 0x00ff66, blending: THREE.AdditiveBlending })
    );
    const ring = new THREE.Mesh(
      new THREE.TorusGeometry(0.22, 0.012, 8, 32),
      new THREE.MeshBasicMaterial({ color: 0x00ff66, blending: THREE.AdditiveBlending, transparent: true, opacity: 0.9 })
    );
    ring.rotation.x = Math.PI / 2;
    this.aimReticle.add(core, ring);
    scene.add(this.aimReticle);

    this.setupControllers();
  }

  setupControllers() {
    for (let i = 0; i < 2; i++) {
      const c = this.renderer.xr.getController(i);
      c.userData.handedness = i === 0 ? 'left' : 'right';
      c.userData.gamepad = null;

      c.addEventListener('connected', (event) => {
        c.userData.handedness = event.data.handedness || c.userData.handedness;
        c.userData.gamepad = event.data.gamepad || null;
      });
      c.addEventListener('disconnected', () => {
        c.userData.gamepad = null;
      });

      const pointer = new THREE.Line(
        new THREE.BufferGeometry().setFromPoints([
          new THREE.Vector3(0, 0, 0),
          new THREE.Vector3(0, 0, -CONFIG.LASER_RANGE)
        ]),
        new THREE.LineBasicMaterial({
          color: i === 0 ? 0x00eaff : 0xff3ff2,
          transparent: true,
          opacity: 0.25,
          blending: THREE.AdditiveBlending
        })
      );
      c.add(pointer);

      const handOrb = new THREE.Mesh(
        new THREE.SphereGeometry(0.05, 12, 8),
        new THREE.MeshBasicMaterial({ color: i === 0 ? 0x00eaff : 0xffd76a, blending: THREE.AdditiveBlending })
      );
      c.add(handOrb);

      this.rig.add(c);
      this.controllers.push(c);
    }
  }

  getRightController() {
    return this.controllers.find(c => c.userData.handedness === 'right') || this.controllers[1] || this.controllers[0];
  }

  getAimRay(playerFacingAngle) {
    const origin = new THREE.Vector3();
    const direction = new THREE.Vector3(0, 0, -1);

    if (this.renderer.xr.isPresenting) {
      const rightCtrl = this.getRightController();
      if (rightCtrl) {
        rightCtrl.getWorldPosition(origin);
        this.tmpMatrix.identity().extractRotation(rightCtrl.matrixWorld);
        direction.applyMatrix4(this.tmpMatrix).normalize();
        return { origin, direction };
      }
    }

    // Desktop: center-screen chase-camera aim.
    this.camera.getWorldPosition(origin);
    this.camera.getWorldDirection(direction);
    direction.normalize();
    return { origin, direction };
  }

  updateAimReticle(aimPoint) {
    this.aimReticle.position.copy(aimPoint);
    this.aimReticle.lookAt(this.rig.position);
    this.aimReticle.rotation.z += 0.04;
  }

  updateChaseRig(avatarPos, facingAngle, dt, isInstant = false) {
    const up = new THREE.Vector3(0, 1, 0);
    const forward = new THREE.Vector3(0, 0, -1).applyAxisAngle(up, facingAngle);
    const tether = new THREE.Vector3(CONFIG.CAM_SIDE, CONFIG.CAM_HEIGHT, CONFIG.CAM_DIST).applyAxisAngle(up, facingAngle);
    const targetPos = avatarPos.clone().add(tether);

    if (isInstant || dt === 0) {
      this.rig.position.copy(targetPos);
    } else {
      this.rig.position.lerp(targetPos, THREE.MathUtils.clamp(dt * 8.0, 0, 1));
    }

    // In desktop mode, force a true third-person camera.
    // In XR, headset pose owns the final camera orientation, so don't fight HMD rotation.
    if (!this.renderer.xr.isPresenting) {
      const lookTarget = avatarPos.clone()
        .add(new THREE.Vector3(0, CONFIG.CAM_LOOK_HEIGHT, 0))
        .addScaledVector(forward, 0.35);
      this.rig.lookAt(lookTarget);
    }

    this.aimReticle.lookAt(this.rig.position);
  }
}
