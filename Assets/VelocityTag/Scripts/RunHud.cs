// RunHud.cs
// Port of hud.js's VRHUD: a panel parented to the active camera showing the
// countdown, the live run, and the results screen.
//
// The results layout follows the hand-off's "results should teach replay" spec
// rather than hud.js's shorter readout — final score, best, medal, tags,
// accuracy, max combo, and the route-bonus breakdown, so a player can see WHY a
// run scored what it did. Every field is already a public property on
// TimeAttackRound; nothing new is computed here.
//
// This is the one file with a TextMeshPro dependency (VelocityTag.asmdef
// references Unity.TextMeshPro). TMP ships inside com.unity.ugui on Unity 6. If
// the Editor ever reports that reference missing, this file and that one line
// are the whole of it — delete both and the rest of the game is unaffected.

using UnityEngine;
using TMPro;
using ShooterCore;

namespace VelocityTag
{
    [DefaultExecutionOrder(200)]   // after the round has ticked this frame
    public class RunHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TimeAttackRound _round;
        [SerializeField] private MatchStateMachine _matchState;

        private float _fuel = 100f;

        private void OnEnable() => GameEventBus.On<PlayerTelemetryEvent>(OnTelemetry);
        private void OnDisable() => GameEventBus.Off<PlayerTelemetryEvent>(OnTelemetry);

        private void OnTelemetry(PlayerTelemetryEvent e) => _fuel = e.Fuel;

        private void LateUpdate()
        {
            if (_label == null || _round == null || _matchState == null) return;
            _label.text = Compose();
        }

        private string Compose()
        {
            switch (_matchState.Phase)
            {
                case MatchPhase.Countdown:
                    int count = Mathf.CeilToInt(_round.Timer);
                    return count > 0 ? $"<size=180%>{count}</size>" : "<size=180%>ENGAGE</size>";

                case MatchPhase.Playing:
                    return $"{FormatClock(_round.Timer)}   SCORE {_round.Score:N0}   " +
                           $"{(_round.Combo > 1 ? $"x{_round.Combo}" : "")}\n" +
                           $"TAGS {_round.Tags}   CHARGES {_round.SuitCharges}   FUEL {Mathf.RoundToInt(_fuel)}%";

                case MatchPhase.Paused:
                    return "<size=150%>PAUSED</size>";

                case MatchPhase.MatchOver:
                    return Results();

                default:
                    return "VELOCITY TAG — TIME ATTACK\nPRESS ENTER / A TO START";
            }
        }

        /// The replay-teaching readout: what you scored, and what earned it.
        private string Results()
        {
            return
                $"<size=150%>{_round.Medal}  {_round.Score:N0}</size>   BEST {_round.BestScore:N0}\n" +
                $"TAGS {_round.Tags}   ACC {_round.Accuracy * 100f:0}%   MAX COMBO x{_round.MaxCombo}\n" +
                $"HELMET {_round.HelmetTags}   FLANK {_round.FlankTags}   AIR {_round.AirTags}   " +
                $"LAUNCH {_round.LaunchTags}   DROP {_round.QuickDropTags}\n" +
                $"HITS TAKEN {_round.HitsTaken}   —   PRESS ENTER / A TO RETRY";
        }

        private static string FormatClock(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int whole = Mathf.FloorToInt(seconds);
            return $"{whole / 60:0}:{whole % 60:00}";
        }
    }
}
