export class InputManager {
  constructor(renderer) {
    this.renderer = renderer;
    this.desktopKeys = {};
    this.buttonMemory = {};
    this.pendingClick = false;

    this.state = {
      planar: { x: 0, z: 0 },
      thrust: 0,
      turn: 0,
      dashPressed: false,
      dropPressed: false,
      firePressed: false,
      startPressed: false,
      menuPressed: false
    };

    window.addEventListener('keydown', (e) => {
      this.desktopKeys[e.code] = true;
      if (e.code === 'Enter' || e.code === 'NumpadEnter') this.pendingClick = true;
      if (e.code === 'KeyH') {
        const info = document.getElementById('info');
        if (info) info.style.display = info.style.display === 'none' ? 'block' : 'none';
      }
    });

    window.addEventListener('keyup', (e) => {
      this.desktopKeys[e.code] = false;
    });

    window.addEventListener('pointerdown', () => {
      if (!this.renderer.xr.isPresenting) this.pendingClick = true;
    });
  }

  getStick(gp) {
    if (!gp || !gp.axes || gp.axes.length < 2) return { x: 0, y: 0 };
    let x = 0, y = 0;
    if (Math.abs(gp.axes[2] || 0) > 0.05 || Math.abs(gp.axes[3] || 0) > 0.05) {
      x = gp.axes[2] || 0;
      y = gp.axes[3] || 0;
    } else {
      x = gp.axes[0] || 0;
      y = gp.axes[1] || 0;
    }
    if (Math.abs(x) < 0.15) x = 0;
    if (Math.abs(y) < 0.15) y = 0;
    return { x, y };
  }

  buttonDown(gp, indexes, threshold = 0.55) {
    if (!gp || !gp.buttons) return false;
    return indexes.some((i) => {
      const b = gp.buttons[i];
      return !!b && (b.pressed || b.value > threshold);
    });
  }

  buttonPressed(hand, actionName, gp, indexes) {
    const key = `${hand}:${actionName}`;
    const down = this.buttonDown(gp, indexes);
    const pressed = down && !this.buttonMemory[key];
    this.buttonMemory[key] = down;
    return pressed;
  }

  keyPressed(code) {
    const down = !!this.desktopKeys[code];
    const key = `kb:${code}`;
    const pressed = down && !this.buttonMemory[key];
    this.buttonMemory[key] = down;
    return pressed;
  }

  update() {
    this.state.planar.x = 0;
    this.state.planar.z = 0;
    this.state.thrust = 0;
    this.state.turn = 0;
    this.state.dashPressed = false;
    this.state.dropPressed = false;
    this.state.firePressed = false;
    this.state.startPressed = false;
    this.state.menuPressed = false;

    const session = this.renderer.xr.getSession && this.renderer.xr.getSession();
    if (session && session.inputSources) {
      for (const source of session.inputSources) {
        const gp = source.gamepad;
        if (!gp) continue;
        const stick = this.getStick(gp);
        const hand = source.handedness || 'unknown';

        if (hand === 'left') {
          this.state.planar.x += stick.x;
          this.state.planar.z += stick.y;
        } else if (hand === 'right') {
          this.state.turn += stick.x;
          this.state.thrust += -stick.y;
        }

        if (this.buttonPressed(hand, 'dash', gp, [1])) this.state.dashPressed = true;
        if (this.buttonPressed(hand, 'drop', gp, [3])) this.state.dropPressed = true;
        if (this.buttonPressed(hand, 'fire', gp, [0])) {
          this.state.firePressed = true;
          this.state.startPressed = true;
        }

        // Fallback for unreliable right-stick vertical axes in Quest Browser:
        // holding A/X also supplies upward jetpack thrust.
        if (this.buttonDown(gp, [4])) this.state.thrust = Math.max(this.state.thrust, 0.75);
      }
    }

    if (this.desktopKeys.KeyW) this.state.planar.z -= 1;
    if (this.desktopKeys.KeyS) this.state.planar.z += 1;
    if (this.desktopKeys.KeyA) this.state.planar.x -= 1;
    if (this.desktopKeys.KeyD) this.state.planar.x += 1;
    if (this.desktopKeys.Space) this.state.thrust += 1;
    if (this.desktopKeys.ShiftLeft || this.desktopKeys.ShiftRight) this.state.thrust -= 1;
    if (this.desktopKeys.KeyQ) this.state.turn -= 1;
    if (this.desktopKeys.KeyE) this.state.turn += 1;

    if (this.keyPressed('KeyF')) this.state.dashPressed = true;
    if (this.keyPressed('KeyC')) this.state.dropPressed = true;
    if (this.keyPressed('Backspace')) this.state.menuPressed = true;

    if (this.pendingClick) {
      this.state.firePressed = true;
      this.state.startPressed = true;
      this.pendingClick = false;
    }
  }
}
