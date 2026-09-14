import * as THREE from 'three';
import { VRButton } from 'three/addons/webxr/VRButton.js';
import GUI from 'lil-gui';
import { CONFIG } from './config.js';
import { InputManager } from './input.js';
import { CameraManager } from './camera.js';
import { Arena } from './arena.js';
import { PlayerAvatar } from './player.js';
import { TargetManager } from './targets.js';
import { CombatManager } from './combat.js';
import { FXManager } from './fx.js';
import { VRHUD } from './hud.js';
import { InVRDebugConsole } from './debugConsole.js';
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
    
    const vrBtn = VRButton.createButton(this.renderer);
    vrBtn.style.zIndex = "10000";
    document.body.appendChild(vrBtn);

    this.clock = new THREE.Clock();
    this.raycaster = new THREE.Raycaster();

    const ambient = new THREE.AmbientLight(0xffffff, 0.8);
    const sun = new THREE.DirectionalLight(0x00eaff, 1.0);
    sun.position.set(10, 20, 10);
    this.scene.add(ambient, sun);

    this.input = new InputManager(this.renderer);
    this.cameraMgr = new CameraManager(this.scene, this.renderer);
    this.arena = new Arena(this.scene);
    this.player = new PlayerAvatar(this.scene);
    this.targets = new TargetManager(this.scene, this.arena);
    this.combat = new CombatManager(this.raycaster, this.targets, this.cameraMgr);
    this.fx = new FXManager(this.scene);
    this.hud = new VRHUD(this.cameraMgr);
    this.debugConsole = new InVRDebugConsole(this.cameraMgr);
    this.round = new RoundManager(this.scene, this.raycaster, this.cameraMgr);

    this.player.group.position.set(0, 0, 12);
    this.cameraMgr.updateChaseRig(this.player.group.position, this.player.facingAngle, 0, true);

    this.setupLiveGUI();
    window.addEventListener('resize', () => this.onResize());
    this.renderer.setAnimationLoop(() => this.animate());
  }

  setupLiveGUI() {
    const gui = new GUI({ title: 'Velocity Tuner' });
    gui.add(CONFIG, 'MOVE_SPEED', 2, 15).name('Run Speed');
    gui.add(CONFIG, 'DASH_SPEED', 10, 40).name('Dash Burst');
    gui.add(CONFIG, 'VERTICAL_THRUST', 5, 30).name('Jetpack');
    gui.close();
  }

  onResize() {
    this.cameraMgr.camera.aspect = window.innerWidth / window.innerHeight;
    this.cameraMgr.camera.updateProjectionMatrix();
    this.renderer.setSize(window.innerWidth, window.innerHeight);
  }

  animate() {
    const dt = Math.min(this.clock.getDelta(), 0.05);
    this.input.update();
    
    const phase = this.round.state.phase;
    const isPlaying = phase === 'playing';

    const aimRay = this.cameraMgr.getAimRay(this.player.facingAngle);
    this.raycaster.set(aimRay.origin, aimRay.direction);
    
    if (phase === 'menu' || phase === 'map_select' || phase === 'gameover') {
       if (this.input.state.firePressed) {
          this.round.handleSelection(this.raycaster);
          this.input.state.firePressed = false; 
       }
    }

    const activeHitZones = this.targets.hitZones.filter(z => {
        const parent = z.userData.parentTarget;
        return parent && !parent.isGhosted && !parent.isDepleted;
    });
    
    const aimHits = this.raycaster.intersectObjects([...activeHitZones, ...this.arena.obstacles], true);
    if (aimHits.length > 0) {
      this.cameraMgr.aimReticle.position.copy(aimHits[0].point);
    } else {
      this.cameraMgr.aimReticle.position.copy(aimRay.origin).addScaledVector(aimRay.direction, 25);
    }

    this.round.update(dt, this.player.group.position, this.player.facingAngle);

    if (isPlaying) {
      this.player.update(this.input.state, this.arena, dt);
      
      // CRITICAL FIX: Ensure active player bounds evaluate continuous solid structure pushbacks
      this.arena.resolveCollisions(this.player.group.position, 0.5);
      
      this.combat.update(dt);
      this.arena.update(this.player, dt);
    }
    
    this.targets.update(this.player.group.position, isPlaying, dt);
    this.cameraMgr.updateChaseRig(this.player.group.position, this.player.facingAngle, dt);
    this.fx.update(dt);
    this.hud.draw(this.round.state);

    this.renderer.render(this.scene, this.cameraMgr.camera);
  }
}

new GameEngine();