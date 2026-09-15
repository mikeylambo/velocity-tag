# Velocity Tag

Unity 6 (6000.5.3f1) URP port of a working Three.js movement-sport prototype.
Quest 3 target, third-person, solo Time Attack. The game is a **score-chase
movement sport**, not a generic shooter — every design call should serve a
repeatable 120-second run the player wants to retry.

## Source of truth: TWO JS builds, not one

`reference/` holds both. They are different tunings of the same game and must
not be blended by accident — this has already happened once.

**`reference/jetpack-lasertag/` — "Time Attack 2.0 Polish". THE TARGET.**
The later build and the one the hand-off describes as current. It alone has the
sport layer: zone scoring, combo, medals, suit charges, ghosting, the 0.75s shot
clock and the 120s round. Its `config.js` is the authority for every value it
defines. `maps/trainingCylinder.js` is its arena (4 platforms, 1 pillar, 4 pads
at power 22, 6 spawns).

**`reference/golden-core-v02/` — "Golden Core v0.2". EARLIER FOUNDATION.**
Its own README says it "is not the final game, it is the stable foundation".
`round.js` is 143 lines against Time Attack's 416; it has no scoring, combo,
medals or charges at all, a 0.18s fire cooldown and a 90s round. Its arena is
different (8 platforms, 4 pillars, pad boost 20.5, recharge radius 2.15).

Rules for using them:

- A value defined in **both** → Time Attack wins. They differ on almost
  everything (ARENA_RADIUS 25 vs 24, ROUND_TIME 120 vs 90, FIRE_COOLDOWN 0.75
  vs 0.18, TURN_SPEED 2.2 vs 2.05, GRAVITY 16.0 vs 16.2, and more).
- A value **only** in Golden Core (DESCEND_SPEED, QUICK_DROP_SPEED, DASH_COST,
  DASH_DURATION, FUEL_REGEN_*, PLAYER_RADIUS, CAM_LOOK_HEIGHT) → usable, but
  label it as cross-build. Time Attack hardcodes its own values for several of
  these in `player.js` (fuel regen 40/12, dash 0.18s cost 20, radius 0.4);
  where it does, those win.
- Behaviour, not just values, can differ. Quick drop is the clearest case:
  Time Attack's `player.js` assigns `velocity.y = -(DASH_SPEED) * 1.5` (-27)
  outright; Golden Core clamps `min(velocity.y, -QUICK_DROP_SPEED)` (-14.5).
  Both are real. Pick deliberately and write down which build you followed.
- **The hand-off export** is third. Where it disagrees with Time Attack's
  config.js — respawn time, ghost window, fire cooldown — config.js wins.

If a value exists in none of the three, **stop and ask**. Do not estimate,
reconstruct, or carry a number over from a similar system. Two real bugs came
from ignoring this: a launch velocity of `14` that appears nowhere (the map says
`22`), and hit-zone offsets derived from prose instead of `targets.js`.

## Hard rules

- **Thrust and gravity are mutually exclusive** in `JetpackLocomotion`. This is
  deliberate and matches `player.js`. Do not "fix" it.
- **Training Cylinder is the official balance map** until the loop sings.
  `triSpireKoth.js` and `reactorRing.js` exist; leave them alone.
- **No new modes.** No KOTH, FFA, TDM, or multiplayer. No parry, ever.
- **Never hand-edit `ProjectSettings/` or `Packages/manifest.json`.** Use the
  Editor, the package manager, or an editor script that goes through the API.
- Prefer small targeted patches over broad rewrites.

## Layout

```
Assets/ShooterCore/     reusable template: event bus, match state machine,
                        GameConfig, chase camera rig, input contract
Assets/VelocityTag/     the game: locomotion, blaster, targets, round scoring,
                        arena, pads, input drivers, camera switch, HUD
Assets/Tests/EditMode/  scoring and hostile-fire suites
reference/              the Three.js build this is ported from
```

`ShooterCore` must not depend on `VelocityTag`. Game-specific decisions
(input mapping, scoring, hit zones) belong in the game layer.

## Working on this

**Rebuild the scene** — `Tools ▸ Velocity Tag ▸ Rebuild Time Attack Scene`.
`TimeAttack.unity` and `TargetDummy.prefab` are generated from
`maps/trainingCylinder.js`, not hand-authored. Change the builder, not the YAML;
hand edits to those two assets are lost on the next rebuild.

**Run the tests** — EditMode, headless:
`-runTests -batchmode -testPlatform EditMode`. CI runs them on every push.

**Start a run** — Enter on desktop, A on the right controller. Same entry point
handles retry from the results screen.

## If you are an agent without a Unity Editor

Most sessions on this repo run remotely with no Editor, so nothing you write is
compile-verified locally. That is a real constraint, not a formality:

- Verify what you can — parse structure, resolve every serialized field an
  editor script assigns against its declaring class, check that JS values you
  transcribed match the file you took them from.
- **Say plainly that a change is not compile-verified.** Do not imply otherwise.
- CI is the gate. Let it go green before merging to `main`.
- Read the JS source before porting behaviour. Prose descriptions — including
  the port's own README — have been wrong where the source was right.

## Deliberately deferred

FX (beams, exhaust, score popups) and audio; results UI beyond the text panel;
the other two maps; networking of any kind. `CooldownWeapon` and
`WeaponDefinition` in ShooterCore are unused by this game and kept as templates.

## Known open question

`hostileFireInterval` in `GameConfig` is the **only** tuned value in the project
with no source. The JS build listens for `HOSTILE_FIRE_INCOMING` but never emits
it, so no shooter was ever written. It is seeded from the hand-off's sport-slice
`ENEMY_FIRE_RATE` (2.5s), which that document explicitly flags as a prototype
number. The brief is pressure, not domination — tune it on purpose.
