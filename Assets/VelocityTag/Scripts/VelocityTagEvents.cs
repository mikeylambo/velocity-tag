// VelocityTagEvents.cs
// Typed events + shared enums for the game layer, named 1:1 with the JS EventBus
// signals. FX, HUD, and results are meant to subscribe to these and stay fully
// decoupled from gameplay logic.

using UnityEngine;

namespace VelocityTag
{
    public enum ZoneType { Chest, Helmet, Flank }
    public enum ReticleState { Ready, Cooldown, TargetLock }
    public enum Medal { None, Bronze, Silver, Gold, Platinum, SRank }

    public struct TimeAttackStartEvent { }
    public struct TimeAttackCountdownTickEvent { public int SecondsLeft; }

    public struct WeaponFiredEvent
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public bool Hit;
        public Vector3 HitPoint;
    }

    // A clean tag on a live target zone. Movement context is captured at the
    // moment of the hit so the round can award route bonuses.
    public struct TargetTaggedEvent
    {
        public ZoneType Zone;
        public Vector3 ImpactPoint;
        public bool Airborne;
        public bool LaunchWindow;
        public bool QuickDropWindow;
        public TargetDummy Target;
    }

    public struct ScoreAwardedEvent
    {
        public int AwardedPoints;
        public int Combo;
        public ZoneType Zone;
        public string BreakdownText; // e.g. "HELMET +150 AIR +50 x3"
        public Vector3 ImpactPoint;
    }

    public struct ComboDroppedEvent { }
    public struct LaunchPadEngagedEvent { public Vector3 PadPosition; }
    public struct SuitChargeChangedEvent { public int Charges; public int MaxCharges; }
    public struct SuitSyncTickEvent { } // reserved for recharge-gate pass

    public struct ReticleStateChangedEvent { public ReticleState State; }

    public struct RunEndedEvent
    {
        public int FinalScore;
        public int BestScore;
        public string Medal;
        public int Tags;
        public float Accuracy;
        public int MaxCombo;
        public int AirTags;
        public int HelmetTags;
        public int FlankTags;
        public int LaunchTags;
        public int HitsTaken;
    }
}
