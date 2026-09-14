import * as THREE from 'three';
import { CONFIG } from './config.js';
import { EventBus } from './eventBus.js';

export class RoundManager {
  constructor(scene, raycaster, cameraManager) {
    this.raycaster = raycaster;
    this.cameraManager = cameraManager;
    
    const bestScore = parseInt(localStorage.getItem('JLT_TIME_ATTACK_CLASSIC_BEST') || '0', 10);
    
    this.state = { 
      mode: 'timeAttackClassic',
      phase: 'menu', // menu | map_select | countdown | playing | paused | results
      timer: 0, 
      score: 0,
      bestScore: bestScore,
      tags: 0,
      shotsFired: 0,
      hitsTaken: 0,
      combo: 0,
      comboTimer: 0,
      maxCombo: 0,
      accuracy: 0,
      
      // Granular performance parameter logs
      helmetTags: 0,
      flankTags: 0,
      airTags: 0,
      launchTags: 0,
      quickDropTags: 0,

      suitCharges: CONFIG.PLAYER_MAX_CHARGES,
      selectedMap: 'trainingCylinder'
    };

    this.selectionCooldown = 0;
    this.buildMenus(scene);
    this.bindEvents();
  }

  setPhase(newPhase) {
    this.state.phase = newPhase;
    EventBus.emit('ROUND_STATE_CHANGED', { phase: this.state.phase });
    this.renderMenuUI();
  }

  updateAccuracy() {
    this.state.accuracy = this.state.shotsFired > 0 
      ? parseFloat(((this.state.tags / this.state.shotsFired) * 100).toFixed(1)) 
      : 0;
  }

  buildMenus(scene) {
    this.menuGroup = new THREE.Group();
    scene.add(this.menuGroup);

    const box = new THREE.Mesh(new THREE.BoxGeometry(4.2, 1.5, 0.2), new THREE.MeshStandardMaterial({ color: 0x050811 }));
    const rim = new THREE.Mesh(new THREE.BoxGeometry(4.3, 1.6, 0.1), new THREE.MeshBasicMaterial({ color: 0x00eaff, wireframe: true }));
    rim.position.z = -0.05;

    this.canvas = document.createElement('canvas'); 
    this.canvas.width = 1024; 
    this.canvas.height = 384;
    this.ctx = this.canvas.getContext('2d');
    this.texture = new THREE.CanvasTexture(this.canvas);
    
    const label = new THREE.Mesh(
      new THREE.PlaneGeometry(4.0, 1.4), 
      new THREE.MeshBasicMaterial({ map: this.texture, transparent: true, blending: THREE.AdditiveBlending })
    );
    label.position.z = 0.12;

    this.menuGroup.add(box, rim, label);

    this.mapButtons = new THREE.Group();
    this.mapButtons.visible = false;
    
    const createBtn = (x, key) => {
        const b = new THREE.Mesh(
          new THREE.BoxGeometry(1.1, 0.5, 0.1), 
          new THREE.MeshBasicMaterial({ color: 0x000000, transparent: true, opacity: 0.0, depthWrite: false })
        );
        b.position.set(x, -0.15, 0.15);
        b.userData = { isMapBtn: true, mapKey: key };
        return b;
    };
    
    this.btnTrain = createBtn(-1.3, 'trainingCylinder');
    this.btnSpire = createBtn(0.0, 'triSpireKoth');
    this.btnRing  = createBtn(1.3, 'reactorRing');
    
    this.mapButtons.add(this.btnTrain, this.btnSpire, this.btnRing);
    this.menuGroup.add(this.mapButtons);

    this.renderMenuUI();
  }

  getMedalRank(score) {
    const medals = CONFIG.TIME_ATTACK_MEDALS || {
      BRONZE: 3000, SILVER: 6000, GOLD: 10000, PLATINUM: 14000, S_RANK: 18000
    };
    if (score >= medals.S_RANK) return { name: 'S-RANK', color: '#bf55ec' };
    if (score >= medals.PLATINUM) return { name: 'PLATINUM', color: '#e5e4e2' };
    if (score >= medals.GOLD) return { name: 'GOLD', color: '#ffd76a' };
    if (score >= medals.SILVER) return { name: 'SILVER', color: '#c0c0c0' };
    if (score >= medals.BRONZE) return { name: 'BRONZE', color: '#cd7f32' };
    return { name: 'UNRANKED', color: '#888888' };
  }

  renderMenuUI() {
    this.ctx.fillStyle = '#000000'; 
    this.ctx.fillRect(0, 0, 1024, 384);
    this.ctx.fillStyle = '#ffffff'; 
    this.ctx.textAlign = 'center';

    if (this.state.phase === 'menu') {
      this.ctx.font = 'bold 64px sans-serif'; 
      this.ctx.fillText('TIME ATTACK CLASSIC', 512, 110);
      this.ctx.font = 'bold 32px sans-serif'; 
      this.ctx.fillStyle = '#00ff66';
      this.ctx.fillText('PULL TRIGGER TO START', 512, 200);
      
      this.ctx.font = 'bold 22px sans-serif'; 
      this.ctx.fillStyle = '#00eaff';
      this.ctx.fillText(`BEST SCORE: ${this.state.bestScore}`, 512, 270);
      
      const medal = this.getMedalRank(this.state.bestScore);
      this.ctx.fillStyle = medal.color;
      this.ctx.fillText(`HIGHEST RANK: ${medal.name}`, 512, 310);
      
      this.mapButtons.visible = false;
    } else if (this.state.phase === 'map_select') {
      this.ctx.font = 'bold 48px sans-serif'; 
      this.ctx.fillText('SELECT ARENA', 512, 70);
      this.ctx.lineWidth = 4;
      
      this.ctx.strokeStyle = '#00eaff';
      this.ctx.strokeRect(50, 150, 280, 120);
      this.ctx.fillStyle = 'rgba(0, 234, 255, 0.15)';
      this.ctx.fillRect(50, 150, 280, 120);
      this.ctx.fillStyle = '#00eaff';
      this.ctx.font = 'bold 24px sans-serif';
      this.ctx.fillText('TRAINING', 190, 220);

      this.ctx.strokeStyle = '#ff245f';
      this.ctx.strokeRect(372, 150, 280, 120);
      this.ctx.fillStyle = 'rgba(255, 36, 95, 0.15)';
      this.ctx.fillRect(372, 150, 280, 120);
      this.ctx.fillStyle = '#ff245f';
      this.ctx.font = 'bold 24px sans-serif';
      this.ctx.fillText('TRI-SPIRE', 512, 220);

      this.ctx.strokeStyle = '#ffd76a';
      this.ctx.strokeRect(694, 150, 280, 120);
      this.ctx.fillStyle = 'rgba(255, 215, 106, 0.15)';
      this.ctx.fillRect(694, 150, 280, 120);
      this.ctx.fillStyle = '#ffd76a';
      this.ctx.font = 'bold 24px sans-serif';
      this.ctx.fillText('REACTOR', 834, 220);

      this.mapButtons.visible = true;
    } else if (this.state.phase === 'paused') {
      this.ctx.font = 'bold 56px sans-serif'; 
      this.ctx.fillStyle = '#ffd76a';
      this.ctx.fillText('SYSTEM PAUSED', 512, 110);
      this.ctx.font = 'bold 28px sans-serif';
      this.ctx.fillStyle = '#ffffff';
      this.ctx.fillText('SHOOT PANEL TO RESUME', 512, 210);
      this.ctx.fillStyle = '#ff245f';
      this.ctx.fillText('PRESS B/Y TO EXIT TO MENU', 512, 290);
      this.mapButtons.visible = false;
    } else if (this.state.phase === 'results') {
      this.ctx.font = 'bold 40px sans-serif'; 
      this.ctx.fillStyle = '#00eaff';
      this.ctx.fillText('TIME ATTACK COMPLETE', 512, 45);
      
      this.ctx.font = 'bold 26px sans-serif';
      this.ctx.fillStyle = '#ffffff'; 
      this.ctx.fillText(`FINAL SCORE: ${this.state.score}`, 512, 85);
      
      const medal = this.getMedalRank(this.state.score);
      this.ctx.fillStyle = medal.color;
      this.ctx.fillText(`MEDAL: ${medal.name}`, 512, 120);
      
      // Extended granular summary reporting complete telemetry context
      this.ctx.font = 'bold 16px sans-serif';
      this.ctx.fillStyle = '#ffffff';
      this.ctx.fillText(`TAGS: ${this.state.tags}  |  MAX COMBO: x${this.state.maxCombo}  |  ACCURACY: ${this.state.accuracy}%`, 512, 155);
      this.ctx.fillStyle = '#ffd76a';
      this.ctx.fillText(`HELMET: ${this.state.helmetTags}  |  FLANK: ${this.state.flankTags}  |  AIR: ${this.state.airTags}`, 512, 182);
      this.ctx.fillStyle = '#00ff66';
      this.ctx.fillText(`LAUNCH: ${this.state.launchTags}  |  QUICK DROP: ${this.state.quickDropTags}`, 512, 205);
      this.ctx.fillStyle = '#ff245f';
      this.ctx.fillText(`HITS TAKEN: ${this.state.hitsTaken}`, 512, 228);

      if (this.state.score > this.state.bestScore && this.state.score > 0) {
        this.ctx.font = 'bold 20px sans-serif'; 
        this.ctx.fillStyle = '#bf55ec';
        this.ctx.fillText(`NEW RECORD! (PREVIOUS: ${this.state.bestScore})`, 512, 262);
      } else {
        this.ctx.font = 'bold 18px sans-serif'; 
        this.ctx.fillStyle = '#888888';
        this.ctx.fillText(`BEST SCORE: ${this.state.bestScore}`, 512, 262);
      }

      this.ctx.font = 'bold 22px sans-serif'; 
      this.ctx.fillStyle = '#00ff66';
      this.ctx.fillText('PULL TRIGGER TO RETRY', 512, 310);
      
      this.mapButtons.visible = false;
    }
    this.texture.needsUpdate = true;
  }

  bindEvents() {
    EventBus.on('TARGET_HIT_REPORT', ({ target, tagType, isAirborne, isLaunchBonus, isQuickDropBonus, impactPoint }) => {
      if (this.state.phase !== 'playing' || target.isGhosted || target.isDepleted) return;

      let basePoints = 0;
      if (tagType === 'CHEST') basePoints = 100;
      else if (tagType === 'HELMET' || tagType === 'HEAD') {
        basePoints = 150;
        this.state.helmetTags += 1;
      }
      else if (tagType === 'FLANK_PACK' || tagType === 'BACK') {
        basePoints = 250;
        this.state.flankTags += 1;
      }

      if (basePoints === 0) return;

      // Ingest multiple contextual scoring layers authoritatively
      let breakdownText = `${tagType} +${basePoints}`;
      if (isAirborne) {
        basePoints += 50;
        this.state.airTags += 1;
        breakdownText += ' | AIR +50';
      }
      if (isLaunchBonus) {
        basePoints += 75;
        this.state.launchTags += 1;
        breakdownText += ' | LAUNCH +75';
      }
      if (isQuickDropBonus) {
        basePoints += 75;
        this.state.quickDropTags += 1;
        breakdownText += ' | QUICK DROP +75';
      }

      // Process rhythm chaining calculations
      const maxComboLimit = CONFIG.MAX_COMBO || 5;
      this.state.combo = THREE.MathUtils.clamp(this.state.combo + 1, 1, maxComboLimit);
      this.state.comboTimer = CONFIG.COMBO_WINDOW || 3.0;
      
      if (this.state.combo > this.state.maxCombo) {
        this.state.maxCombo = this.state.combo;
      }
      
      const awardedPoints = basePoints * this.state.combo;
      if (this.state.combo > 1) {
        breakdownText += ` | COMBO x${this.state.combo}`;
      }

      this.state.score += awardedPoints;
      this.state.tags += 1;
      this.updateAccuracy();

      EventBus.emit('TARGET_HIT_CONFIRMED', { target });
      EventBus.emit('ADD_SCORE', { 
        basePoints, 
        points: awardedPoints, 
        combo: this.state.combo, 
        tagType,
        breakdownText,
        impactPoint 
      });
    });

    EventBus.on('WEAPON_FIRED', () => {
      if (this.state.phase === 'playing') {
        this.state.shotsFired += 1;
        this.updateAccuracy();
      }
    });

    EventBus.on('SUIT_SYNC_TICK', () => {
      if (this.state.phase === 'playing' && this.state.suitCharges < CONFIG.PLAYER_MAX_CHARGES) {
        this.state.suitCharges = Math.min(CONFIG.PLAYER_MAX_CHARGES, this.state.suitCharges + 1);
        EventBus.emit('LOG_DEBUG', { message: `SUIT INTEGRITY RESTORED: ${this.state.suitCharges}/${CONFIG.PLAYER_MAX_CHARGES}` });
      }
    });

    EventBus.on('PLAYER_HIT', () => {
      if (this.state.phase !== 'playing') return;

      this.state.hitsTaken += 1;
      this.state.score = Math.max(0, this.state.score - (CONFIG.PLAYER_HIT_PENALTY || 250));
      this.state.combo = 0;
      this.state.comboTimer = 0;
      this.state.suitCharges -= 1;
      
      EventBus.emit('COMBO_DROPPED');

      if (this.state.suitCharges <= 0) {
        this.state.score = Math.max(0, this.state.score - (CONFIG.ZERO_INTEGRITY_PENALTY || 1000));
        this.state.suitCharges = CONFIG.PLAYER_MAX_CHARGES;
        EventBus.emit('LOG_DEBUG', { message: 'RECALIBRATION: Suit integrity reset' });
      }
    });

    EventBus.on('TOGGLE_PAUSE', () => {
      if (this.state.phase === 'playing') {
        this.selectionCooldown = 0.5;
        this.setPhase('paused');
        EventBus.emit('LOG_DEBUG', { message: 'GAME LOOP PAUSED' });
      } else if (this.state.phase === 'paused') {
        this.selectionCooldown = 0.5;
        this.setPhase('map_select');
      }
    });
  }

  handleSelection(raycaster) {
    if (this.selectionCooldown > 0) return;

    if (this.state.phase === 'menu' || this.state.phase === 'results') {
        this.selectionCooldown = 0.5;
        this.setPhase('map_select');
    } else if (this.state.phase === 'map_select') {
        const hits = raycaster.intersectObjects(this.mapButtons.children);
        if (hits.length > 0) {
            this.state.selectedMap = hits[0].object.userData.mapKey;
            this.startTimeAttackClassic();
        }
    } else if (this.state.phase === 'paused') {
        this.menuGroup.visible = false;
        this.setPhase('playing');
    }
  }

  startTimeAttackClassic() {
    this.state.mode = 'timeAttackClassic';
    this.state.timer = CONFIG.TIME_ATTACK_COUNTDOWN || 3;
    this.state.score = 0;
    this.state.tags = 0;
    this.state.shotsFired = 0;
    this.state.hitsTaken = 0;
    this.state.combo = 0;
    this.state.comboTimer = 0;
    this.state.maxCombo = 0;
    this.state.accuracy = 0;
    
    this.state.helmetTags = 0;
    this.state.flankTags = 0;
    this.state.airTags = 0;
    this.state.launchTags = 0;
    this.state.quickDropTags = 0;

    this.state.suitCharges = CONFIG.PLAYER_MAX_CHARGES;

    this.menuGroup.visible = false;
    this.setPhase('countdown');
    
    EventBus.emit('TIME_ATTACK_START');
    EventBus.emit('ROUND_START', { map: this.state.selectedMap });
  }

  endTimeAttackClassic() {
    this.state.timer = 0;
    this.selectionCooldown = 0.5;

    if (this.state.score > this.state.bestScore) {
      this.state.bestScore = this.state.score;
      localStorage.setItem('JLT_TIME_ATTACK_CLASSIC_BEST', String(this.state.bestScore));
    }

    this.setPhase('results');
    EventBus.emit('TIME_ATTACK_END');
    EventBus.emit('ROUND_END');
  }

  update(dt, playerPos, facingAngle) {
    if (this.selectionCooldown > 0) {
      this.selectionCooldown -= dt;
    }

    const currentPhase = this.state.phase;
    if (currentPhase === 'menu' || currentPhase === 'map_select' || currentPhase === 'paused' || currentPhase === 'results') {
      this.menuGroup.visible = true;
      const forward = new THREE.Vector3(0, 0, -4.5).applyAxisAngle(new THREE.Vector3(0,1,0), facingAngle);
      this.menuGroup.position.copy(playerPos).add(forward);
      this.menuGroup.position.y = THREE.MathUtils.clamp(playerPos.y + 1.8, 1.5, 12);
      this.menuGroup.lookAt(playerPos.x, this.menuGroup.position.y, playerPos.z);
    } else if (currentPhase === 'countdown') {
      this.state.timer -= dt;
      if (this.state.timer <= 0) {
        this.state.timer = CONFIG.TIME_ATTACK_DURATION || 120;
        this.setPhase('playing');
      }
    } else if (currentPhase === 'playing') {
      this.state.timer -= dt;
      
      if (this.state.combo > 0) {
        this.state.comboTimer -= dt;
        if (this.state.comboTimer <= 0) {
          this.state.combo = 0;
          EventBus.emit('COMBO_DROPPED');
        }
      }

      if (this.state.timer <= 0) {
        this.endTimeAttackClassic();
      }
    }
  }
}