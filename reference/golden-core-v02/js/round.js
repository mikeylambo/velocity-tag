import * as THREE from 'three';
import { CONFIG } from './config.js';

export class RoundManager {
  constructor(scene, raycaster) {
    this.raycaster = raycaster;
    this.state = {
      phase: 'menu',
      timer: 0,
      score: 0,
      tags: 0,
      shots: 0,
      lastTag: '',
      announce: 'AIM & SHOOT TO START'
    };

    this.buildStartMenu(scene);
  }

  buildStartMenu(scene) {
    this.menuGroup = new THREE.Group();
    this.menuGroup.position.set(0, 2.55, 3.8);
    scene.add(this.menuGroup);

    const panelMat = new THREE.MeshStandardMaterial({ color: 0x0b1022, roughness: 0.4, metalness: 0.6 });
    const rimMat = new THREE.MeshBasicMaterial({ color: 0x00eaff, blending: THREE.AdditiveBlending, transparent: true, opacity: 0.9 });

    const panel = new THREE.Mesh(new THREE.BoxGeometry(4.5, 2.1, 0.12), panelMat);
    const rim = new THREE.Mesh(new THREE.BoxGeometry(4.65, 2.25, 0.05), rimMat);
    rim.position.z = -0.05;
    this.menuGroup.add(panel, rim);

    this.label = this.makeText('JETPACK LASER TAG', 4.2, 0.56, 48, '#00eaff');
    this.label.position.set(0, 0.55, 0.09);
    this.menuGroup.add(this.label);

    this.startLabel = this.makeText('SHOOT / CLICK / ENTER TO START', 4.05, 0.45, 34, '#ffffff');
    this.startLabel.position.set(0, -0.16, 0.1);
    this.menuGroup.add(this.startLabel);

    this.tipLabel = this.makeText('RUN → LAUNCH → TAG → DROP → RECHARGE', 4.1, 0.38, 25, '#ffd76a');
    this.tipLabel.position.set(0, -0.72, 0.1);
    this.menuGroup.add(this.tipLabel);

    this.startButtonBox = panel;
  }

  makeText(text, w, h, size, color) {
    const canvas = document.createElement('canvas');
    canvas.width = 768;
    canvas.height = 192;
    const ctx = canvas.getContext('2d');
    ctx.fillStyle = '#000000';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    ctx.fillStyle = color;
    ctx.font = `bold ${size}px system-ui, sans-serif`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(text, canvas.width / 2, canvas.height / 2);

    const tex = new THREE.CanvasTexture(canvas);
    return new THREE.Mesh(
      new THREE.PlaneGeometry(w, h),
      new THREE.MeshBasicMaterial({ map: tex, transparent: true, blending: THREE.AdditiveBlending })
    );
  }

  addScore(amount, tagType) {
    this.state.score += amount;
    this.state.tags += 1;
    this.state.lastTag = tagType;
    this.state.announce = `${tagType} TAG +${amount}`;
  }

  startCountdown() {
    this.state.phase = 'countdown';
    this.state.timer = CONFIG.COUNTDOWN_TIME;
    this.state.score = 0;
    this.state.tags = 0;
    this.state.shots = 0;
    this.state.lastTag = '';
    this.state.announce = 'READY';
    this.menuGroup.visible = false;
  }

  forceMenu() {
    this.state.phase = 'gameover';
    this.state.timer = 0;
    this.state.announce = 'ROUND ENDED';
    this.menuGroup.visible = true;
  }

  update(dt, inputState, cameraMgr, player, targetMgr, combatMgr) {
    if (this.state.phase === 'menu' || this.state.phase === 'gameover') {
      if (this.menuGroup && !this.menuGroup.visible) this.menuGroup.visible = true;
      this.positionMenu(cameraMgr.rig.position, player.group.position);

      if (inputState.startPressed) {
        this.startCountdown();
        player.reset();
        targetMgr.resetAll();
        combatMgr.reset();
      }
      return;
    }

    if (this.state.phase === 'countdown') {
      this.state.timer -= dt;
      if (this.state.timer <= 0) {
        this.state.phase = 'playing';
        this.state.timer = CONFIG.ROUND_TIME;
        this.state.announce = 'GO!';
      }
      return;
    }

    if (this.state.phase === 'playing') {
      if (inputState.menuPressed) {
        this.forceMenu();
        return;
      }

      this.state.timer -= dt;
      if (this.state.timer <= 0) {
        this.state.phase = 'gameover';
        this.state.timer = 0;
        this.state.announce = `FINAL SCORE ${this.state.score}`;
        this.menuGroup.visible = true;
      }
    }
  }

  positionMenu(cameraPos, playerPos) {
    const toCamera = cameraPos.clone().sub(playerPos);
    toCamera.y = 0;
    if (toCamera.lengthSq() < 0.01) toCamera.set(0, 0, 1);
    toCamera.normalize();

    this.menuGroup.position.copy(playerPos).addScaledVector(toCamera, 4.2);
    this.menuGroup.position.y = Math.max(2.4, playerPos.y + 2.2);
    this.menuGroup.lookAt(cameraPos);
  }
}
