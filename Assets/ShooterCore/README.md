# Unity Templates — from Neon Galactic Arena + Velocity Tag

## Honest starting point

These two codebases are not peers. **Velocity Tag is a real, working game** —
jetpack physics, cooldown-gated raycast combat, a chase camera with a genuine
VR gimbal-lock fix, an event bus wiring ~15 systems together, VR + desktop
input, FX, in-VR HUD. **Neon Galactic Arena, as uploaded, is an architecture
plan with no implementation** — every file is a `// TODO: port from the
original main.js` stub; the working 1069-line prototype it's meant to absorb
isn't in this repo.

So this isn't "merge two working systems." It's: Velocity Tag proved several
systems work in practice; Neon Galactic's own docs independently sketched the
same system boundaries (state machine, input abstraction, weapon registry,
net interface) without building them. Both pointing at the same shape is a
good signal these are the right things to template — so that's what's here,
built from Velocity Tag's real logic, shaped to the cleaner contracts Neon
Galactic's docs described.

## What's in here

| File | Ported from | What changed |
|---|---|---|
| `Core/GameEventBus.cs` | Velocity Tag's `eventBus.js` (working, ~15 event types) | Typed struct events instead of string + object, so the compiler catches mismatches |
| `Core/MatchStateMachine.cs` | Velocity Tag's `round.js` phases + Neon Galactic's `MatchState.js` shape | Merged into one enum: both games' phases were actually the same idea at different completeness |
| `Core/GameConfig.cs` | Velocity Tag's `config.js` (real, playtested numbers) | ScriptableObject — replaces the `lil-gui` runtime tuner with the Inspector, for free |
| `Combat/WeaponDefinition.cs` | Neon Galactic's `WeaponDefs.js` (12-weapon data, no logic) | Data schema, now paired with logic that exists |
| `Combat/CooldownWeapon.cs` | Velocity Tag's `combat.js` `executeAimFire()` (the only combat logic that's actually implemented anywhere) | Reads a `WeaponDefinition` instead of one hardcoded blaster |
| `Camera/ChaseCameraRig.cs` | Velocity Tag's `camera.js` `updateChaseRig()` | Preserves the explicit-Euler-over-LookAt fix exactly — this one's load-bearing, don't "clean it up" back to `LookAt()` |
| `Input/CrossPlatformInput.cs` | Neon Galactic's `InputState.js` contract + Velocity Tag's working PC/XR polling | Contract from one file, working logic from the other |

## What's NOT in here (real gaps, not oversights)

- **No networking.** Velocity Tag has none at all (solo Time Attack). Neon
  Galactic has an interface (`Connection.js`) but no implementation. Nothing
  to port yet — build this once, directly in Unity (Netcode for GameObjects
  or a relay service), rather than porting from either.
- **No target/hit-zone system.** Velocity Tag's `targets.js` (200 lines —
  ghosting, respawn logic, hit-zone tagging) is real and working but is
  gameplay-specific enough (dummy-tag-sport rules) that it wasn't
  generalized here — port it directly per-project rather than templating it.
- **No FX/particle system.** Velocity Tag's `fx.js` exists and works; not
  included here since Unity's particle system (Shuriken/VFX Graph) is a
  better home for this than a ported JS module would be.
- **CrossPlatformInput's XR button/axis mappings are placeholders.** Unity's
  `InputDevice`/`CommonUsages` API doesn't map 1:1 to raw WebXR gamepad
  indices — verify trigger/grip/thumbstick-click mappings against your
  actual Quest 3 hardware before trusting this in a build, the same way
  Velocity Tag's own `input.js` comments flag its `axes[2]/[3]` fix as a
  real hardware gotcha, not a guess.

## Suggested next step

Prototype fast in the browser (as you've been doing), port whichever
mechanic proves out into these templates as the permanent, reusable Unity
asset — per the workflow you landed on earlier.
