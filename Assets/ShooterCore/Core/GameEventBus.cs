// GameEventBus.cs
// Generalized from Velocity Tag's src/eventBus.js — the actual, working pub/sub
// backbone that decouples PlayerAvatar, CombatManager, CameraManager, FXManager,
// and RoundManager from each other in the JS build (INPUT_FIRE, PLAYER_HIT,
// ROUND_STATE_CHANGED, SPAWN_EXHAUST, etc. all flow through it).
//
// Neon Galactic's architecture doc doesn't have a literal EventBus file, but
// every module boundary it describes (input/ never touching weapons/ directly,
// net/ being the only thing that changes when swapping backends) is the same
// decoupling goal. This is that pattern, done once, for both games and any
// future shooter in the catalog.
//
// Usage:
//   GameEventBus.On<PlayerHitEvent>(OnPlayerHit);
//   GameEventBus.Emit(new PlayerHitEvent { Damage = 50 });
//   GameEventBus.Off<PlayerHitEvent>(OnPlayerHit);
//
// Struct-based events (not strings) so the compiler catches typos and payload
// shape mismatches — the JS version's `EventBus.emit('event-name', payload)`
// has no such guardrail, which is worth fixing on the port rather than porting
// verbatim.

using System;
using System.Collections.Generic;

namespace ShooterCore
{
    public static class GameEventBus
    {
        private static readonly Dictionary<Type, Delegate> _listeners = new Dictionary<Type, Delegate>();

        public static void On<T>(Action<T> callback)
        {
            var type = typeof(T);
            if (_listeners.TryGetValue(type, out var existing))
            {
                _listeners[type] = Delegate.Combine(existing, callback);
            }
            else
            {
                _listeners[type] = callback;
            }
        }

        public static void Off<T>(Action<T> callback)
        {
            var type = typeof(T);
            if (_listeners.TryGetValue(type, out var existing))
            {
                var result = Delegate.Remove(existing, callback);
                if (result == null) _listeners.Remove(type);
                else _listeners[type] = result;
            }
        }

        public static void Emit<T>(T payload)
        {
            var type = typeof(T);
            if (_listeners.TryGetValue(type, out var existing) && existing is Action<T> action)
            {
                // Mirrors eventBus.js's try/catch-per-listener: one bad handler
                // shouldn't take down every other system listening for this event.
                foreach (var handler in action.GetInvocationList())
                {
                    try { ((Action<T>)handler)(payload); }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError($"GameEventBus error on {type.Name}: {e}");
                    }
                }
            }
        }

        // Call on scene teardown / match-over to avoid stale listeners leaking
        // across scenes — the JS version doesn't need this (page reload clears
        // everything), Unity does.
        public static void ClearAll() => _listeners.Clear();
    }

    // --- Example event payloads, generalized from both games' actual event names ---

    public struct PlayerHitEvent { public int Damage; public UnityEngine.Vector3 ImpactPoint; }
    public struct WeaponFiredEvent { public string WeaponId; public UnityEngine.Vector3 Origin; public UnityEngine.Vector3 Direction; }
    public struct TargetHitEvent { public UnityEngine.GameObject Target; public string TagType; public bool IsAirborne; public bool IsBonusWindow; }
    public struct RoundPhaseChangedEvent { public MatchPhase Phase; }
    public struct PlayerTelemetryEvent { public UnityEngine.Vector3 Position; public float Fuel; public bool IsGrounded; }
}
