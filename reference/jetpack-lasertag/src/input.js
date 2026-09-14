import { EventBus } from './eventBus.js';

export class InputManager {
  constructor(renderer) {
    this.renderer = renderer;
    this.state = {
      planar: { x: 0, z: 0 },
      turn: 0,
      thrust: 0,
      firePressed: false
    };
    
    this.mem = { dash: false, pause: false, fire: false, quickDrop: false };
    this.setupDesktopInteractions();
  }

  setupDesktopInteractions() {
    // Standard Desktop Mappings
    window.addEventListener('keydown', (e) => {
      if (e.code === 'KeyW') this.state.planar.z = 1;
      if (e.code === 'KeyS') this.state.planar.z = -1;
      if (e.code === 'KeyD') this.state.planar.x = 1;
      if (e.code === 'KeyA') this.state.planar.x = -1;
      if (e.code === 'ArrowRight') this.state.turn = 1;
      if (e.code === 'ArrowLeft') this.state.turn = -1;
      if (e.code === 'Space') this.state.thrust = 1;
      if (e.code === 'ShiftLeft') EventBus.emit('INPUT_DASH');
      if (e.code === 'KeyC' || e.code === 'ControlLeft') EventBus.emit('EXECUTE_QUICK_DROP');
      if (e.code === 'KeyP' || e.code === 'Escape') EventBus.emit('TOGGLE_PAUSE');
    });

    window.addEventListener('keyup', (e) => {
      if (e.code === 'KeyW' || e.code === 'KeyS') this.state.planar.z = 0;
      if (e.code === 'KeyA' || e.code === 'KeyD') this.state.planar.x = 0;
      if (e.code === 'ArrowLeft' || e.code === 'ArrowRight') this.state.turn = 0;
      if (e.code === 'Space') this.state.thrust = 0;
    });

    window.addEventListener('mousedown', () => {
      this.state.firePressed = true;
      EventBus.emit('INPUT_FIRE');
    });

    window.addEventListener('mouseup', () => {
      this.state.firePressed = false;
    });
  }

  update() {
    if (!this.renderer || !this.renderer.xr || !this.renderer.xr.isPresenting) return;
    const session = this.renderer.xr.getSession();
    if (!session || !session.inputSources) return;

    this.state.planar.x = 0;
    this.state.planar.z = 0;
    this.state.turn = 0;
    this.state.thrust = 0;

    for (const source of session.inputSources) {
      if (!source.gamepad) continue;
      const gp = source.gamepad;

      // Primary Aiming Arm (Right Controller)
      if (source.handedness === 'right') {
        if (Math.abs(gp.axes[2]) > 0.15) this.state.turn = gp.axes[2];
        
        // Primary Trigger Evaluation
        const triggerValue = gp.buttons[0]?.value || 0;
        if (triggerValue > 0.5 && !this.mem.fire) {
          this.mem.fire = true;
          this.state.firePressed = true;
          EventBus.emit('INPUT_FIRE');
        } else if (triggerValue <= 0.1) {
          this.mem.fire = false;
          this.state.firePressed = false;
        }

        // QUICK DROP TRIGGER: Polling Right Thumbstick Click (buttons[3])
        const stickClicked = gp.buttons[3]?.pressed;
        if (stickClicked && !this.mem.quickDrop) {
          this.mem.quickDrop = true;
          EventBus.emit('EXECUTE_QUICK_DROP');
          EventBus.emit('LOG_DEBUG', { message: 'QUICK DROP TRIGGERED' });
        } else if (!stickClicked) {
          this.mem.quickDrop = false;
        }

        // System interruption interface (B button mapping)
        if (gp.buttons[5]?.pressed && !this.mem.pause) {
          this.mem.pause = true;
          EventBus.emit('TOGGLE_PAUSE');
        } else if (!gp.buttons[5]?.pressed) {
          this.mem.pause = false;
        }
      }

      // Mobility Chassis Arm (Left Controller)
      if (source.handedness === 'left') {
        if (Math.abs(gp.axes[2]) > 0.15) this.state.planar.x = gp.axes[2];
        if (Math.abs(gp.axes[3]) > 0.15) this.state.planar.z = -gp.axes[3];

        this.state.thrust = gp.buttons[0]?.value || 0;

        if (gp.buttons[1]?.pressed && !this.mem.dash) {
          this.mem.dash = true;
          EventBus.emit('INPUT_DASH');
        } else if (!gp.buttons[1]?.pressed) {
          this.mem.dash = false;
        }

        if (gp.buttons[5]?.pressed && !this.mem.pause) {
          this.mem.pause = true;
          EventBus.emit('TOGGLE_PAUSE');
        } else if (!gp.buttons[5]?.pressed) {
          this.mem.pause = false;
        }
      }
    }
  }
}