import * as THREE from 'three';
import { CONFIG } from './config.js';
import { EventBus } from './eventBus.js';

export class VRHUD {
  constructor(cameraManager) {
    this.canvas = document.createElement('canvas');
    this.canvas.width = 1024; 
    this.canvas.height = 256;
    this.ctx = this.canvas.getContext('2d');
    this.texture = new THREE.CanvasTexture(this.canvas);
    
    this.fuel = 100;
    this.fireCooldown = 0;

    this.mesh = new THREE.Mesh(
      new THREE.PlaneGeometry(2.0, 0.5),
      new THREE.MeshBasicMaterial({ 
        map: this.texture, 
        transparent: true, 
        blending: THREE.AdditiveBlending, 
        depthTest: false,
        depthWrite: false
      })
    );
    
    this.mesh.renderOrder = 9999;
    this.mesh.position.set(0, -0.65, -2.5);
    cameraManager.camera.add(this.mesh);

    this.bindEvents();
  }

  bindEvents() {
    EventBus.on('PLAYER_TELEMETRY', ({ fuel }) => { this.fuel = fuel; });
    EventBus.on('WEAPON_FIRED', () => { this.fireCooldown = CONFIG.PLAYER_FIRE_COOLDOWN || 0.75; });
  }

  draw(roundState) {
    this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
    this.ctx.fillStyle = 'rgba(5, 7, 20, 0.85)';
    this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);

    this.ctx.strokeStyle = '#00eaff'; 
    this.ctx.lineWidth = 4;
    this.ctx.strokeRect(4, 4, this.canvas.width - 8, this.canvas.height - 8);

    if (this.fireCooldown > 0 && roundState.phase === 'playing') {
      this.fireCooldown = Math.max(0, this.fireCooldown - 0.016);
    }

    if (roundState.phase === 'countdown') {
      const countVal = Math.ceil(roundState.timer);
      this.ctx.font = 'bold 100px sans-serif';
      this.ctx.textAlign = 'center';
      this.ctx.fillStyle = countVal > 0 ? '#00eaff' : '#00ff66';
      this.ctx.fillText(countVal > 0 ? countVal : 'ENGAGE', this.canvas.width / 2, 155);
    } else if (roundState.phase === 'playing') {
      this.ctx.textAlign = 'left';
      this.ctx.font = 'bold 28px sans-serif';
      
      const timeLeft = Math.max(0, Math.ceil(roundState.timer));
      this.ctx.fillStyle = timeLeft <= 15 ? '#ff245f' : '#ffffff';
      this.ctx.fillText(`TIME: ${timeLeft}s`, 30, 50);
      
      this.ctx.fillStyle = '#00ff66';
      this.ctx.fillText(`SCORE: ${roundState.score}`, 220, 50);
      
      this.ctx.fillStyle = '#ffd76a';
      this.ctx.fillText(`COMBO: x${roundState.combo > 0 ? roundState.combo : 1}`, 460, 50);

      this.ctx.fillStyle = '#00eaff';
      this.ctx.fillText(`BEST: ${roundState.bestScore}`, 720, 50);

      this.ctx.fillStyle = '#ffffff';
      this.ctx.font = 'bold 18px sans-serif';
      this.ctx.fillText(`INTEGRITY:`, 30, 110);
      for (let i = 0; i < CONFIG.PLAYER_MAX_CHARGES; i++) {
        this.ctx.beginPath();
        this.ctx.arc(170 + (i * 25), 104, 8, 0, Math.PI * 2);
        if (i < (roundState.suitCharges || 0)) {
          this.ctx.fillStyle = '#00eaff';
          this.ctx.fill();
        } else {
          this.ctx.strokeStyle = '#ff245f';
          this.ctx.lineWidth = 2;
          this.ctx.stroke();
        }
      }

      const isReady = this.fireCooldown <= 0;
      this.ctx.fillStyle = isReady ? '#00ff66' : '#bf55ec';
      this.ctx.font = 'bold 20px sans-serif';
      this.ctx.fillText(isReady ? 'SHOT READY' : 'RECHARGING', 320, 110);

      this.ctx.strokeStyle = this.fuel < 20 ? '#ff245f' : '#bf55ec';
      this.ctx.strokeRect(500, 90, 300, 24);
      this.ctx.fillStyle = this.fuel < 20 ? '#ff245f' : '#bf55ec';
      this.ctx.fillRect(504, 94, (this.fuel / 100) * 292, 16);
      this.ctx.fillStyle = '#ffffff';
      this.ctx.font = 'bold 16px sans-serif';
      this.ctx.fillText(`THRUSTER ${Math.round(this.fuel)}%`, 820, 108);

    } else if (roundState.phase === 'results') {
      // Minimal layout mapping straight during summary presentation overlays
      this.ctx.textAlign = 'center';
      this.ctx.font = 'bold 36px sans-serif';
      this.ctx.fillStyle = '#00eaff';
      this.ctx.fillText('TIME ATTACK COMPLETE', this.canvas.width / 2, 45);

      this.ctx.font = 'bold 22px sans-serif';
      this.ctx.fillStyle = '#ffffff';
      this.ctx.fillText(`FINAL SCORE: ${roundState.score}   |   BEST SCORE: ${roundState.bestScore}`, this.canvas.width / 2, 90);
      
      this.ctx.font = 'bold 18px sans-serif';
      this.ctx.fillText(`TAGS: ${roundState.tags}  |  SHOTS FIRED: ${roundState.shotsFired}  |  ACCURACY: ${roundState.accuracy}%`, this.canvas.width / 2, 130);
      this.ctx.fillText(`MAX COMBO: x${roundState.maxCombo}  |  HITS TAKEN: ${roundState.hitsTaken}`, this.canvas.width / 2, 160);

      this.ctx.fillStyle = '#00ff66';
      this.ctx.font = 'bold 20px sans-serif';
      this.ctx.fillText('PULL TRIGGER TO RETRY', this.canvas.width / 2, 230);
    } else {
      this.ctx.textAlign = 'center';
      this.ctx.font = 'bold 32px sans-serif';
      this.ctx.fillStyle = '#00ff66';
      this.ctx.fillText('SYSTEM READY', this.canvas.width / 2, 130);
    }

    this.texture.needsUpdate = true;
  }
}