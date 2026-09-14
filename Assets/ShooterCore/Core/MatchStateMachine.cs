// MatchStateMachine.cs
// Unifies two versions of the same idea that both games independently arrived at:
//   - Neon Galactic's core/MatchState.js: MENU -> PLAYING -> PAUSED -> MATCH_OVER
//   - Velocity Tag's round.js phases (read from round.state.phase in main.js):
//     'menu' | 'map_select' | 'playing' | 'gameover'
//
// Velocity Tag's version is the one that's actually wired up and driving a real
// game loop (main.js gates player.update / combat.update / arena collisions on
// `phase === 'playing'`). Neon Galactic's is the cleaner *shape* (explicit enum,
// typed listeners) but was never implemented. This merges them: the superset of
// phases both games need, with the typed-listener API Neon Galactic's stub wanted.
//
// One extra phase (MAP_SELECT) is included since Velocity Tag proved it's needed
// in practice — worth keeping for any future shooter that has a map-vote step.

using UnityEngine;

namespace ShooterCore
{
    public enum MatchPhase
    {
        Menu,
        MapSelect,
        Countdown,   // Velocity Tag's TIME_ATTACK_COUNTDOWN (3s pre-round beat) — its
                     // own explicit phase rather than folded into Playing, so HUD/input
                     // can react differently during it (e.g. lock firing).
        Playing,
        Paused,
        MatchOver
    }

    public class MatchStateMachine : MonoBehaviour
    {
        [SerializeField] private MatchPhase _phase = MatchPhase.Menu;
        [SerializeField] private string _mode = "FFA";   // FFA | TDM | TIME_ATTACK | 1v1 | 2v2
        [SerializeField] private string _map = "ARENA";

        public MatchPhase Phase => _phase;
        public string Mode { get => _mode; set => _mode = value; }
        public string Map { get => _map; set => _map = value; }

        public bool IsPlaying => _phase == MatchPhase.Playing;

        public void SetPhase(MatchPhase next)
        {
            if (_phase == next) return;
            _phase = next;
            GameEventBus.Emit(new RoundPhaseChangedEvent { Phase = next });
        }

        // Convenience wrappers matching the JS naming both games used, so a
        // JS-to-C# port reads 1:1 rather than needing a mental remap.
        public void StartCountdown() => SetPhase(MatchPhase.Countdown);
        public void StartMatch() => SetPhase(MatchPhase.Playing);
        public void Pause() { if (_phase == MatchPhase.Playing) SetPhase(MatchPhase.Paused); }
        public void Resume() { if (_phase == MatchPhase.Paused) SetPhase(MatchPhase.Playing); }
        public void EndMatch() => SetPhase(MatchPhase.MatchOver);
        public void ReturnToMenu() => SetPhase(MatchPhase.Menu);
    }
}
