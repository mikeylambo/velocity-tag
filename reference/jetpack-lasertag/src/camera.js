import * as THREE from 'three';
import { CONFIG } from './config.js';

export class CameraManager {
  constructor(scene, renderer) {
    this.rig = new THREE.Group();
    // Parenting camera to rig for chase behavior
    this.camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.05, 1000);
    this.rig.add(this.camera);
    scene.add(this.rig);

    this.controllers = [];
    this.aimReticle = new THREE.Mesh(
      new THREE.SphereGeometry(0.1, 12, 8),
      new THREE.MeshBasicMaterial({ color: 0x00ff66, blending: THREE.AdditiveBlending, wireframe: true })
    );
    scene.add(this.aimReticle);

    this.setupControllers(renderer);
  }

  setupControllers(renderer) {
    for (let i = 0; i < 2; i++) {
      const c = renderer.xr.getController(i);
      
      // Initializing handedness based on index; updated by connected event if available
      c.userData.handedness = i === 0 ? 'left' : 'right';

      const pointer = new THREE.Line(
        new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(), new THREE.Vector3(0, 0, -CONFIG.LASER_RANGE)]),
        new THREE.LineBasicMaterial({ 
          color: i === 0 ? 0x00eaff : 0xff3ff2, 
          transparent: true, 
          opacity: 0.3, 
          blending: THREE.AdditiveBlending 
        })
      );
      c.add(pointer);
      this.rig.add(c);
      this.controllers.push(c);

      // Listen for actual connection to correct handedness mapping
      c.addEventListener('connected', (event) => {
        c.userData.handedness = event.data.handedness;
        pointer.material.color.setHex(event.data.handedness === 'right' ? 0xff3ff2 : 0x00eaff);
      });
    }
  }

  getAimRay(playerFacingAngle) {
    const origin = new THREE.Vector3();
    const direction = new THREE.Vector3(0, 0, -1);

    // Prioritize the right controller for aiming
    const rightCtrl = this.controllers.find(c => 
      c.userData.handedness === 'right' && c.parent && c.visible && c.matrixWorld.elements[0] !== 0
    );
    
    if (rightCtrl) {
      rightCtrl.getWorldPosition(origin);
      const tmpMat = new THREE.Matrix4().identity().extractRotation(rightCtrl.matrixWorld);
      direction.applyMatrix4(tmpMat).normalize();
    } else {
      // Use rig/camera orientation as fallback
      this.camera.getWorldPosition(origin);
      this.camera.getWorldDirection(direction);
    }
    
    return { origin, direction };
  }

  updateChaseRig(avatarPos, facingAngle, dt, isInstant = false) {
    // 1. Calculate the tether position behind the player
    const tether = new THREE.Vector3(CONFIG.CAM_SIDE, CONFIG.CAM_HEIGHT, CONFIG.CAM_DIST)
      .applyAxisAngle(new THREE.Vector3(0, 1, 0), facingAngle);
    
    const targetPos = avatarPos.clone().add(tether);

    // 2. Smoothly lerp position
    if (isInstant) {
      this.rig.position.copy(targetPos);
    } else {
      this.rig.position.lerp(targetPos, THREE.MathUtils.clamp(dt * 8.0, 0, 1));
    }

    // 3. FIX: Replace rig.lookAt with explicit Euler rotation to avoid VR flip loops.
    // We match the player's yaw (facingAngle) and apply a fixed pitch offset.
    this.rig.rotation.set(CONFIG.CAM_PITCH_OFFSET || -0.15, facingAngle, 0, 'YXZ');

    // Reticle always faces camera for consistent scaling appearance
    this.aimReticle.lookAt(this.camera.getWorldPosition(new THREE.Vector3()));
  }
}