import * as THREE from 'three';
import { EventBus } from './eventBus.js';

export class FXManager {
  constructor(scene) {
    this.scene = scene;
    this.activeNodes = [];

    // Pre-compiled object pool geometries mitigating runtime pipeline stutter
    this.geoBeam = new THREE.CylinderGeometry(0.04, 0.04, 1, 8);
    this.geoBeam.applyMatrix4(new THREE.Matrix4().makeTranslation(0, 0.5, 0));
    this.geoBeam.applyMatrix4(new THREE.Matrix4().makeRotationX(Math.PI / 2));

    this.geoExhaust = new THREE.SphereGeometry(0.25, 8, 8);
    this.geoRing = new THREE.RingGeometry(0.1, 0.3, 12);

    this.matBeamPlayer = new THREE.MeshBasicMaterial({ color: 0x00eaff, blending: THREE.AdditiveBlending });
    this.matBeamHostile = new THREE.MeshBasicMaterial({ color: 0xff245f, blending: THREE.AdditiveBlending });
    this.matExhaust = new THREE.MeshBasicMaterial({ color: 0xffd76a, transparent: true, opacity: 0.6, blending: THREE.AdditiveBlending });
    this.matRing = new THREE.MeshBasicMaterial({ color: 0xffffff, side: THREE.DoubleSide, transparent: true, opacity: 0.9, blending: THREE.AdditiveBlending });

    // Dynamic 2D Text Canvas caching system to generate high-contrast popups instantly
    this.canvasPool = document.createElement('canvas');
    this.canvasPool.width = 256;
    this.canvasPool.height = 64;
    this.ctxPool = this.canvasPool.getContext('2d');

    this.bindEvents();
  }

  bindEvents() {
    EventBus.on('SPAWN_BEAM', ({ start, end, color }) => {
      const material = color === 0xff245f ? this.matBeamHostile : this.matBeamPlayer;
      const mesh = new THREE.Mesh(this.geoBeam, material);
      mesh.position.copy(start);
      mesh.lookAt(end);
      mesh.scale.set(1, 1, start.distanceTo(end));
      
      this.scene.add(mesh);
      this.pushNode({ mesh, timer: 0.08, type: 'beam' });
    });

    EventBus.on('SPAWN_EXHAUST', ({ position }) => {
      const mesh = new THREE.Mesh(this.geoExhaust, this.matExhaust);
      mesh.position.copy(position);
      mesh.position.x += (Math.random() - 0.5) * 0.2;
      mesh.position.z += (Math.random() - 0.5) * 0.2;
      
      this.scene.add(mesh);
      this.pushNode({ mesh, timer: 0.15, type: 'exhaust' });
    });

    // SATISFYING SCORING FEEDBACK: Generates localized spatial text floating instantly over direct tags
    EventBus.on('ADD_SCORE', ({ breakdownText, impactPoint }) => {
      if (!impactPoint || !breakdownText) return;

      // Draw customized canvas overlay string frames
      this.ctxPool.clearRect(0, 0, 256, 64);
      this.ctxPool.font = '900 22px sans-serif';
      this.ctxPool.textAlign = 'center';
      this.ctxPool.fillStyle = '#000000';
      this.ctxPool.fillText(breakdownText, 128, 38);
      this.ctxPool.fillStyle = breakdownText.includes('HELMET') ? '#ffd76a' : (breakdownText.includes('FLANK') ? '#bf55ec' : '#00ff66');
      this.ctxPool.fillText(breakdownText, 126, 36);

      const cachedTex = new THREE.CanvasTexture(this.canvasPool);
      const textMesh = new THREE.Mesh(
        new THREE.PlaneGeometry(1.6, 0.4),
        new THREE.MeshBasicMaterial({ map: cachedTex, transparent: true, blending: THREE.AdditiveBlending, depthWrite: false })
      );
      
      textMesh.position.copy(impactPoint).add(new THREE.Vector3(0, 0.6, 0));
      this.scene.add(textMesh);
      this.pushNode({ mesh: textMesh, timer: 0.8, type: 'text', tex: cachedTex });

      // Impact Shockwave Ring
      const ringMesh = new THREE.Mesh(this.geoRing, this.matRing.clone());
      ringMesh.position.copy(impactPoint);
      ringMesh.lookAt(this.scene.position);
      this.scene.add(ringMesh);
      this.pushNode({ mesh: ringMesh, timer: 0.25, type: 'ring' });
    });
  }

  pushNode(nodeData) {
    this.activeNodes.push(nodeData);
    if (this.activeNodes.length > 50) {
      const stale = this.activeNodes.shift();
      stale.mesh.removeFromParent();
      if (stale.tex) stale.tex.dispose();
      if (stale.mesh.material && stale.type === 'ring') stale.mesh.material.dispose();
    }
  }

  update(dt) {
    for (let i = this.activeNodes.length - 1; i >= 0; i--) {
      const node = this.activeNodes[i];
      node.timer -= dt;

      if (node.type === 'exhaust') {
        const factor = node.timer / 0.15;
        node.mesh.scale.setScalar(factor);
      } else if (node.type === 'text') {
        node.mesh.position.y += dt * 1.5; // Float upwards gently
        node.mesh.material.opacity = node.timer / 0.8;
      } else if (node.type === 'ring') {
        const expand = 1.0 + (0.25 - node.timer) * 8.0;
        node.mesh.scale.setScalar(expand);
        node.mesh.material.opacity = (node.timer / 0.25) * 0.9;
      }

      if (node.timer <= 0) {
        node.mesh.removeFromParent();
        if (node.tex) node.tex.dispose();
        if (node.mesh.material && node.type === 'ring') node.mesh.material.dispose();
        this.activeNodes.splice(i, 1);
      }
    }
  }
}