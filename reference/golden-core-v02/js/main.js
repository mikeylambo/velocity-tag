import * as THREE from 'three';
import { VRButton } from 'three/addons/webxr/VRButton.js';

import { InputManager } from './input.js';
import { CameraManager } from './camera.js';
import { Arena } from './arena.js';
import { PlayerAvatar } from './player.js';
import { TargetManager } from './targets.js';
import { CombatManager } from './combat.js';
import { FXManager } from './fx.js';
import { VRHUD } from './hud.js';
import { RoundManager } from './round.js';

class GameEngine {
  constructor() {
    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x020308);
    this.scene.fog = new THREE.FogExp2(0x020308, 0.018);

    this.renderer = new THREE.WebGLRenderer({ antialias: true, powerPreference: 'high-performance' });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.renderer.setSize(window.innerWidth, window.innerHeight);
    this.renderer.xr.enabled = true;
    this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
    document.body.appendChild(this.renderer.domElement);
    document.body.appendChild(VRButton.createButton(this.renderer));

    this.clock = new THREE.Clock();
    this.raycaster = new THREE.Raycaster();

    this.addLights();

    this.input = new InputManager(this.renderer);
    this.cameraMgr = new CameraManager(this.scene, this.renderer);
    this.arena = new Arena(this.scene);
    this.player = new PlayerAvatar(this.scene);
    this.targets = new TargetManager(this.scene);
    this.combat = new CombatManager(this.raycaster);
    this.fx = new FXManager(this.scene);
    this.hud = new VRHUD(this.cameraMgr.camera);
    this.round = new RoundManager(this.scene, this.raycaster);

    this.cameraMgr.updateChaseRig(this.player.group.position, this.player.facingAngle, 0, true);

    window.addEventListener('resize', () => this.onResize());
    this.renderer.setAnimationLoop(() => this.animate());
  }

  addLights() {
    const hemi = new THREE.HemisphereLight(0x0e7bff, 0xff1d80, 1.35);
    this.scene.add(hemi);

    const key = new THREE.DirectionalLight(0xffffff, 1.65);
    key.position.set(0, 25, 8);
    this.scene.add(key);
  }

  onResize() {
    this.cameraMgr.camera.aspect = window.innerWidth / window.innerHeight;
    this.cameraMgr.camera.updateProjectionMatrix();
    this.renderer.setSize(window.innerWidth, window.innerHeight);
  }

  updateAim() {
    const aimRay = this.cameraMgr.getAimRay(this.player.facingAngle);
    this.raycaster.set(aimRay.origin, aimRay.direction);
    this.raycaster.far = 64;

    const liveZones = this.targets.hitZones.filter(z => z.visible && z.userData.parentTarget.active);
    const aimHits = this.raycaster.intersectObjects([...liveZones, ...this.arena.obstacles], true);

    const aimPoint = aimHits.length
      ? aimHits[0].point.clone()
      : aimRay.origin.clone().addScaledVector(aimRay.direction, 25);

    this.cameraMgr.updateAimReticle(aimPoint);
  }

  animate() {
    const dt = Math.min(this.clock.getDelta(), 0.05);
    const elapsed = this.clock.elapsedTime;

    this.input.update();
    this.updateAim();

    this.round.update(dt, this.input.state, this.cameraMgr, this.player, this.targets, this.combat);
    this.arena.update(dt);

    if (this.round.state.phase === 'playing') {
      this.player.update(this.input.state, this.arena, this.fx, dt);
      this.targets.update(this.player.group.position, dt, elapsed);
      this.combat.processFire(
        this.input.state,
        this.cameraMgr,
        this.player,
        this.arena,
        this.targets,
        this.round,
        this.fx,
        elapsed
      );
    } else {
      // Keep camera and targets readable during menus/countdowns.
      this.targets.update(this.player.group.position, dt, elapsed);
    }

    this.cameraMgr.updateChaseRig(this.player.group.position, this.player.facingAngle, dt);
    this.fx.update(dt, this.cameraMgr.rig.position);
    this.hud.draw(this.round.state, this.player.fuel, this.player.isGrounded, this.player.onRechargePad);

    this.renderer.render(this.scene, this.cameraMgr.camera);
  }
}

new GameEngine();
