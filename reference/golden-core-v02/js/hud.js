import * as THREE from 'three';

export class VRHUD {
  constructor(camera) {
    this.canvas = document.createElement('canvas');
    this.canvas.width = 1024;
    this.canvas.height = 256;
    this.ctx = this.canvas.getContext('2d');
    this.texture = new THREE.CanvasTexture(this.canvas);

    this.mesh = new THREE.Mesh(
      new THREE.PlaneGeometry(1.95, 0.49),
      new THREE.MeshBasicMaterial({ map: this.texture, transparent: true, blending: THREE.AdditiveBlending, depthTest: false })
    );
    this.mesh.position.set(0, -0.62, -1.85);
    this.mesh.renderOrder = 999;
    camera.add(this.mesh);
  }

  draw(roundState, playerFuel, playerGrounded, onRechargePad) {
    const ctx = this.ctx;
    ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
    ctx.fillStyle = 'rgba(2, 3, 8, 0.78)';
    ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
    ctx.strokeStyle = onRechargePad ? '#00ff66' : '#00eaff';
    ctx.lineWidth = 4;
    ctx.strokeRect(6, 6, this.canvas.width - 12, this.canvas.height - 12);

    if (roundState.phase === 'countdown') {
      const countVal = Math.ceil(roundState.timer);
      const label = countVal > 0 ? countVal.toString() : 'GO!';
      ctx.fillStyle = label === 'GO!' ? '#00ff66' : '#ffd76a';
      ctx.font = 'bold 120px system-ui, sans-serif';
      ctx.textAlign = 'center';
      ctx.fillText(label, 512, 162);
      ctx.fillStyle = '#d6ffff';
      ctx.font = 'bold 22px system-ui, sans-serif';
      ctx.fillText('Prepare to launch.', 512, 218);
      this.texture.needsUpdate = true;
      document.getElementById('score2d').textContent = `COUNTDOWN ${label}`;
      return;
    }

    if (roundState.phase === 'menu' || roundState.phase === 'gameover') {
      ctx.fillStyle = '#00eaff';
      ctx.font = 'bold 46px system-ui, sans-serif';
      ctx.textAlign = 'center';
      const msg = roundState.phase === 'gameover' ? `ROUND OVER: ${roundState.score} PTS` : 'GOLDEN CORE';
      ctx.fillText(msg, 512, 106);
      ctx.fillStyle = '#ffffff';
      ctx.font = 'bold 24px system-ui, sans-serif';
      ctx.fillText('Shoot the floating panel or click/Enter to start.', 512, 156);
      ctx.fillStyle = '#ffd76a';
      ctx.fillText('Test: movement → launch pads → chest/helmet tags.', 512, 200);
      this.texture.needsUpdate = true;
      document.getElementById('score2d').textContent = roundState.phase === 'gameover'
        ? `FINAL ${roundState.score}`
        : 'SYSTEM STANDBY';
      return;
    }

    ctx.textAlign = 'left';
    ctx.font = 'bold 36px system-ui, sans-serif';
    ctx.fillStyle = '#00eaff';
    ctx.fillText(`SCORE ${roundState.score}`, 28, 55);
    ctx.fillText(`TAGS ${roundState.tags}`, 335, 55);

    const timeLeft = Math.max(0, Math.ceil(roundState.timer));
    ctx.fillStyle = timeLeft < 15 ? '#ff245f' : '#ffd76a';
    ctx.fillText(`TIME ${timeLeft}s`, 780, 55);

    ctx.strokeStyle = playerFuel < 20 ? '#ff245f' : '#00ff66';
    ctx.strokeRect(30, 112, 420, 32);
    ctx.fillStyle = playerFuel < 20 ? '#ff245f' : '#00ff66';
    ctx.fillRect(34, 116, Math.max(0, Math.min(1, playerFuel / 100)) * 412, 24);

    ctx.fillStyle = '#ffffff';
    ctx.font = 'bold 20px system-ui, sans-serif';
    ctx.fillText(`JETPACK FUEL ${Math.round(playerFuel)}%`, 30, 100);

    ctx.fillStyle = onRechargePad ? '#00ff66' : (playerGrounded ? '#88ffee' : '#ffd76a');
    ctx.font = 'bold 24px system-ui, sans-serif';
    ctx.fillText(onRechargePad ? 'RECHARGE PAD ACTIVE' : (playerGrounded ? 'GROUNDED: FAST RECHARGE' : 'AIRBORNE TAG BONUS +50'), 30, 192);

    if (roundState.announce) {
      ctx.fillStyle = '#ffd76a';
      ctx.font = 'bold 28px system-ui, sans-serif';
      ctx.fillText(roundState.announce, 520, 192);
    }

    ctx.fillStyle = '#d6ffff';
    ctx.font = 'bold 16px system-ui, sans-serif';
    ctx.fillText('Chest +100 | Helmet +150 | Air tag +50 | Launch pads test vertical flow', 30, 232);

    this.texture.needsUpdate = true;
    document.getElementById('score2d').textContent = `SCORE ${roundState.score} | TIME ${timeLeft}s`;
  }
}
