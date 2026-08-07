// MatchStateMachine.cs
// The menu -> countdown -> playing -> results flow, ported from the JS round
// state handling. TimeAttackRound drives phase transitions; everything else
// reads Phase / IsPlaying.

using UnityEngine;

namespace ShooterCore
{
    public enum MatchPhase { Menu, Countdown, Playing, Results }

    public class MatchStateMachine : MonoBehaviour
    {
        public MatchPhase Phase { get; private set; } = MatchPhase.Menu;
        public bool IsPlaying => Phase == MatchPhase.Playing;

        public void SetPhase(MatchPhase phase)
        {
            if (Phase == phase) return;
            Phase = phase;
            GameEventBus.Emit(new MatchPhaseChangedEvent { Phase = phase });
        }
    }

    public struct MatchPhaseChangedEvent { public MatchPhase Phase; }
}
