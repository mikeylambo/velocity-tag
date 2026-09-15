import * as THREE from 'three';

export class FXManager {
  constructor(scene) {
    this.scene = scene;
    this.bursts = [];
    this.beams = [];
    this.floaters = [];

    this.matJet = new THREE.MeshBasicMaterial({ color: 0x00eaff, blending: THREE.AdditiveBlending, transparent: true, opacity: 0.95 });
  }

  spawnLaserBeam(start, end, colorHex) {
    const mat = new THREE.LineBasicMaterial({ color: colorHex, blending: THREE.AdditiveBlending, transparent: true, opacity: 1 });
    const beam = new THREE.Line(new THREE.BufferGeometry().setFromPoints([start, end]), mat);
    this.scene.add(beam);
    this.beams.push({ mesh: beam, life: 0.10 });
  }

  spawnHitBurst(pos, colorHex) {
    const group = new THREE.Group();
    group.position.copy(pos);
    const mat = new THREE.MeshBasicMaterial({ color: colorHex, blending: THREE.AdditiveBlending, transparent: true, opacity: 1 });
    const geo = new THREE.OctahedronGeometry(0.1, 0);

    for (let i = 0; i < 14; i++) {
      const p = new THREE.Mesh(geo, mat);
      p.userData.vel = new THREE.Vector3(Math.random() - 0.5, Math.random() - 0.2, Math.random() - 0.5)
        .normalize()
        .multiplyScalar(3.0 + Math.random() * 3.5);
      group.add(p);
    }

    this.scene.add(group);
    this.bursts.push({ group, life: 0.42 });
  }

  spawnJetExhaust(player) {
    const p = new THREE.Mesh(new THREE.BoxGeometry(0.12, 0.12, 0.12), this.matJet.clone());
    p.position.copy(player.group.position).add(new THREE.Vector3((Math.random() - 0.5) * 0.28, 0.78, 0.35));
    this.scene.add(p);
    this.floaters.push({ mesh: p, life: 0.18, vel: new THREE.Vector3(0, -1.1, 0), billboard: false });
  }

  spawnPadBoost(padPos) {
    this.spawnHitBurst(new THREE.Vector3(padPos.x, 0.5, padPos.z), 0xffa526);
    this.spawnFloatingText('LAUNCH', new THREE.Vector3(padPos.x, 1.2, padPos.z), '#ffd76a');
  }

  spawnFloatingText(text, pos, color = '#ffffff') {
    const canvas = document.createElement('canvas');
    canvas.width = 512;
    canvas.height = 128;
    const ctx = canvas.getContext('2d');
    ctx.fillStyle = '#000000';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    ctx.fillStyle = color;
    ctx.font = 'bold 46px system-ui, sans-serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(text, 256, 64);

    const tex = new THREE.CanvasTexture(canvas);
    const mesh = new THREE.Mesh(
      new THREE.PlaneGeometry(1.5, 0.4),
      new THREE.MeshBasicMaterial({ map: tex, transparent: true, blending: THREE.AdditiveBlending, opacity: 1 })
    );
    mesh.position.copy(pos);
    this.scene.add(mesh);
    this.floaters.push({ mesh, life: 0.85, vel: new THREE.Vector3(0, 1.2, 0), billboard: true });
  }

  update(dt, cameraPos) {
    for (let i = this.bursts.length - 1; i >= 0; i--) {
      const b = this.bursts[i];
      b.life -= dt;
      b.group.children.forEach(p => p.position.addScaledVector(p.userData.vel, dt));
      b.group.scale.multiplyScalar(1 + dt * 2.0);
      if (b.life <= 0) {
        this.scene.remove(b.group);
        b.group.traverse(o => {
          if (o.geometry) o.geometry.dispose();
          if (o.material) o.material.dispose();
        });
        this.bursts.splice(i, 1);
      }
    }

    for (let i = this.beams.length - 1; i >= 0; i--) {
      const bm = this.beams[i];
      bm.life -= dt;
      bm.mesh.material.opacity = Math.max(0, bm.life * 10);
      if (bm.life <= 0) {
        this.scene.remove(bm.mesh);
        bm.mesh.geometry.dispose();
        bm.mesh.material.dispose();
        this.beams.splice(i, 1);
      }
    }

    for (let i = this.floaters.length - 1; i >= 0; i--) {
      const ft = this.floaters[i];
      ft.life -= dt;
      ft.mesh.position.addScaledVector(ft.vel, dt);
      if (ft.billboard) ft.mesh.lookAt(cameraPos);
      if (ft.mesh.material) ft.mesh.material.opacity = Math.max(0, ft.life / 0.85);

      if (ft.life <= 0) {
        this.scene.remove(ft.mesh);
        if (ft.mesh.geometry) ft.mesh.geometry.dispose();
        if (ft.mesh.material) ft.mesh.material.dispose();
        this.floaters.splice(i, 1);
      }
    }
  }
}
