import * as THREE from 'three';
import { EventBus } from './eventBus.js';

export class InVRDebugConsole {
  constructor(cameraManager) {
    this.canvas = document.createElement('canvas');
    this.canvas.width = 512; 
    this.canvas.height = 256;
    this.ctx = this.canvas.getContext('2d');
    this.texture = new THREE.CanvasTexture(this.canvas);
    this.logs = [];

    // Construct high-contrast display slate
    this.mesh = new THREE.Mesh(
      new THREE.PlaneGeometry(0.3, 0.15),
      new THREE.MeshBasicMaterial({ map: this.texture, transparent: true, opacity: 0.85, depthTest: false })
    );
    this.mesh.renderOrder = 999;
    this.mesh.rotation.x = -Math.PI / 2;
    this.mesh.position.set(0, 0.08, 0.05); // Aligned above wrist joint

    // Mount natively to the left WebXR controller rig space
    this.attachToController(cameraManager);
    this.interceptConsole();
  }

  attachToController(cameraManager) {
    const leftCtrl = cameraManager.controllers.find(c => c.userData.handedness === 'left');
    if (leftCtrl) {
      leftCtrl.add(this.mesh);
    } else {
      // Fallback alignment directly to camera chassis view
      this.mesh.position.set(-0.3, -0.3, -1.0);
      this.mesh.rotation.set(0, 0, 0);
      cameraManager.rig.add(this.mesh);
    }
  }

  interceptConsole() {
    const defaultLog = console.log.bind(console);
    const defaultWarn = console.warn.bind(console);
    const defaultErr = console.error.bind(console);

    const pushLog = (type, msg) => {
      if (typeof msg === 'object') msg = JSON.stringify(msg);
      this.logs.push(`[${type}] ${msg}`);
      if (this.logs.length > 8) this.logs.shift();
      this.render();
    };

    console.log = (...args) => { defaultLog(...args); pushLog('LOG', args.join(' ')); };
    console.warn = (...args) => { defaultWarn(...args); pushLog('WARN', args.join(' ')); };
    console.error = (...args) => { defaultErr(...args); pushLog('ERR', args.join(' ')); };

    EventBus.on('LOG_DEBUG', ({ message }) => console.log(message));
  }

  render() {
    this.ctx.fillStyle = '#050714';
    this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
    this.ctx.strokeStyle = '#00eaff';
    this.ctx.lineWidth = 4;
    this.ctx.strokeRect(2, 2, this.canvas.width - 4, this.canvas.height - 4);

    this.ctx.fillStyle = '#ffffff';
    this.ctx.font = 'bold 18px monospace';
    this.ctx.fillText('SYSTEM TELEMETRY LOGS', 14, 25);

    this.ctx.font = '14px monospace';
    this.logs.forEach((log, index) => {
      this.ctx.fillStyle = log.includes('[ERR]') ? '#ff245f' : (log.includes('[WARN]') ? '#ffd76a' : '#00ff66');
      this.ctx.fillText(log, 14, 55 + (index * 22));
    });
    this.texture.needsUpdate = true;
  }
}