// VelocityTagEvents.cs
// Typed event payloads for Velocity Tag's game layer, mirroring the string
// events in the JS build's eventBus wiring 1:1 so the port is auditable:
//   INPUT_DASH / EXECUTE_QUICK_DROP / PLAYER_TELEMETRY / TARGET_HIT_REPORT /
//   TARGET_HIT_CONFIRMED / ADD_SCORE / SUIT_SYNC_TICK / COMBO_DROPPED /
//   LAUNCH ROUTE ENGAGED (was a LOG_DEBUG string match — now a real event).
// Structural events (PlayerHitEvent, WeaponFiredEvent, RoundPhaseChangedEvent,
// PlayerTelemetryEvent) already live in ShooterCore.GameEventBus.

using UnityEngine;

namespace VelocityTag
{
    public enum TagType { Chest, Helmet, FlankPack }

    public struct DashEvent { }
    public struct QuickDropEvent { }

    /// Replaces the JS hack of sniffing LOG_DEBUG for 'LAUNCH ROUTE ENGAGED'.
    public struct LaunchPadEngagedEvent { public Vector3 PadPosition; }

    /// combat.js -> round.js contact report. Round is the single scoring authority.
    public struct TargetHitReportEvent
    {
        public TargetDummy Target;
        public TagType Zone;
        public bool IsAirborne;
        public bool IsLaunchBonus;
        public bool IsQuickDropBonus;
        public Vector3 ImpactPoint;
    }

    /// round.js -> targets.js confirmation that scoring accepted the hit.
    public struct TargetHitConfirmedEvent { public TargetDummy Target; }

    /// round.js ADD_SCORE — consumed by HUD/FX for popups.
    public struct ScoreAwardedEvent
    {
        public int BasePoints;
        public int AwardedPoints;
        public int Combo;
        public TagType Zone;
        public string BreakdownText;
        public Vector3 ImpactPoint;
    }

    public struct SuitSyncTickEvent { }   // recharge gate pulse
    public struct ComboDroppedEvent { }
    public struct TimeAttackStartEvent { }
    public struct TimeAttackEndEvent { }
}
