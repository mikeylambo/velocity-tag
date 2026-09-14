# Velocity Tag — Unity Port (Drop-In Package)

Faithful C# port of the "Time Attack 2.0 Polish" JS build (player.js, combat.js,
targets.js, round.js) sitting on top of the ShooterCore templates. All tuned
values live in the GameConfig ScriptableObject — no magic numbers changed in port.

## What's here

```
Assets/
├── ShooterCore/            # your shared templates, unchanged
│   ├── Core/               #   GameEventBus, MatchStateMachine, GameConfig
│   ├── Combat/             #   WeaponDefinition, CooldownWeapon
│   ├── Camera/             #   ChaseCameraRig (gimbal-safe)
│   └── Input/              #   CrossPlatformInput (Quest 3 axis fix included)
└── VelocityTag/Scripts/    # the game layer (this port)
    ├── VelocityTagEvents.cs    # typed events, 1:1 with the JS EventBus names
    ├── JetpackLocomotion.cs    # player.js physics: thrust/gravity/dash/quick-drop/fuel
    ├── TagBlaster.cs           # combat.js: shot clock, raycast, bonus windows, reticle state
    ├── TargetDummy.cs          # targets.js: 1-charge ghost/flash/recoil/patrol
    ├── TargetZone.cs           # hit-zone marker (Chest/Helmet/FlankPack)
    ├── TargetSpawnNetwork.cs   # spawn points + 5.0-unit reposition rule
    ├── TimeAttackRound.cs      # round.js: scoring authority, combo, suit charges, medals
    ├── ArenaBounds.cs          # floor query + obstacle push-out
    └── LaunchPad.cs            # trigger pad, arms 2s launch bonus
```

## Day-one pipeline check (do before scene work)

1. Unity 6000.x, Universal 3D (URP) template — matches your Hub screenshot.
2. Package Manager: install **XR Interaction Toolkit**, **XR Plug-in Management**,
   enable **OpenXR** for Android, add the **Meta Quest Support** OpenXR feature group.
3. Switch platform to Android, set texture compression ASTC.
4. Build the empty scene to Quest 3 over USB. If this works, everything else is iteration.

## Scene wiring (Training Cylinder equivalent)

1. Create the `GameConfig` asset: right-click → Create → Shooter → Game Config.
   Defaults already match config.js — verify playerFireCooldown 0.75, ghost 0.75.
2. Empty GO **"Match"** → add `MatchStateMachine` + `TimeAttackRound` (assign config).
3. Empty GO **"Arena"** → add `ArenaBounds`; build floor/platforms/pillars as
   primitives; assign platform + obstacle colliders to its lists.
   Launch pads: cylinder trigger + `LaunchPad`.
4. **Player** GO → `JetpackLocomotion` + `CrossPlatformInput` + `TagBlaster`
   (+ `ChaseCameraRig` for desktop testing; XR Origin replaces it in VR).
   TagBlaster aim origin = camera (desktop) / right controller (VR).
5. **TargetDummy prefab**: body/chest/head/back boxes matching JS proportions
   (body 0.55×0.75×0.35, chest 0.45×0.3×0.1 front, head 0.35×0.1×0.1 top,
   back 0.3×0.4×0.12 rear). Each zone: BoxCollider + `TargetZone` (set enum),
   all on a **"TargetZones" layer** → set that layer as TagBlaster's mask.
6. Empty GO **"Spawns"** with child empties at the trainingCylinder.js spawn
   coordinates → assign to `TargetSpawnNetwork` along with prefab/config/player.
7. A menu button (or debug key) calls `TimeAttackRound.StartTimeAttack()`.

## Input contract for this game (per-game driver decision)

Map into `NormalizedInputState` as: **Strafe = planar** (x strafe, y fwd/back),
**Thrust = vertical jetpack 0–1**, **Look.x = smooth turn**, Firing = trigger,
DashPressed = grip, QuickDropPressed = right-stick click. On press, emit
`DashEvent` / `QuickDropEvent`; on Firing, call `TagBlaster.TryFire()`.

## Deliberately deferred (rebuild natively, don't port)

- FX (beams, exhaust, score popups): listen to WeaponFiredEvent / ScoreAwardedEvent /
  ComboDroppedEvent with Unity particles + TextMeshPro.
- HUD/results: read TimeAttackRound's public properties (incl. Medal + Accuracy).
- Hostile fire: dummies are passive in this first build; add after feel is proven.
- Recharge gates: trigger volume emitting SuitSyncTickEvent on a 1.5s cooldown.
