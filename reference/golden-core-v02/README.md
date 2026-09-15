# Jetpack Laser Tag: Golden Core v0.2

A cleaned-up multi-file WebXR/desktop prototype.

## Run locally

Because ES modules and WebXR need a local/secure server, do not open `index.html` directly from the filesystem.

From this folder:

```bash
python -m http.server 8080
```

Then open:

```text
http://localhost:8080
```

For Quest testing, host over HTTPS using CodePen, Netlify, Vercel, GitHub Pages, itch.io HTML upload, or a local tunnel.

## Controls

### Desktop
- Click / Enter: start or shoot
- WASD: move
- Q/E: turn
- Space / Shift: jetpack up/down
- F: dash
- C: quick drop
- Backspace: return to menu
- H: hide/show instruction panel

### Quest
- Trigger: shoot/select
- Left stick: move
- Right stick X: turn
- Right stick Y: jetpack thrust
- A/X hold: upward thrust fallback
- Grip: dash
- Stick click: quick drop

## Design Goal

Golden Core is not the final game. It is the stable foundation:

Run → launch → hover → tag → drop → recharge → relaunch.
