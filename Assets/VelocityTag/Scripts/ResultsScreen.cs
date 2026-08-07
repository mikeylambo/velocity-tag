// ResultsScreen.cs
// Minimal results panel fed by RunEndedEvent (the payload round.js built the
// results screen from). Shows final score vs best, the medal, and the
// telemetry line-items; PLAY AGAIN re-arms the run via the Button wired to
// TimeAttackRound.StartTimeAttack. Also owns hiding the menu START button
// once a run is queued. Pure presentation.

using UnityEngine;
using UnityEngine.UI;
using ShooterCore;

namespace VelocityTag
{
    public class ResultsScreen : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private GameObject _startButton; // menu START, hidden once a run begins
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _medalText;
        [SerializeField] private Text _statsText;

        private void OnEnable()
        {
            GameEventBus.On<RunEndedEvent>(OnRunEnded);
            GameEventBus.On<MatchPhaseChangedEvent>(OnPhaseChanged);
        }

        private void OnDisable()
        {
            GameEventBus.Off<RunEndedEvent>(OnRunEnded);
            GameEventBus.Off<MatchPhaseChangedEvent>(OnPhaseChanged);
        }

        private void Start()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnPhaseChanged(MatchPhaseChangedEvent e)
        {
            if (e.Phase == MatchPhase.Countdown)
            {
                if (_startButton != null) _startButton.SetActive(false);
                if (_panel != null) _panel.SetActive(false);
            }
        }

        private void OnRunEnded(RunEndedEvent e)
        {
            if (_scoreText != null)
            {
                bool newBest = e.FinalScore > 0 && e.FinalScore >= e.BestScore;
                _scoreText.text = e.FinalScore.ToString("N0")
                    + (newBest ? "  —  NEW BEST" : "  —  BEST " + e.BestScore.ToString("N0"));
            }

            if (_medalText != null)
                _medalText.text = e.Medal == "None" ? "" : e.Medal.ToUpper() + " MEDAL";

            if (_statsText != null)
                _statsText.text =
                    "TAGS " + e.Tags
                    + "   ACCURACY " + Mathf.RoundToInt(e.Accuracy * 100f) + "%"
                    + "   MAX COMBO x" + e.MaxCombo + "\n"
                    + "HELMET " + e.HelmetTags
                    + "   FLANK " + e.FlankTags
                    + "   AIR " + e.AirTags
                    + "   LAUNCH " + e.LaunchTags
                    + "   HITS " + e.HitsTaken;

            if (_panel != null) _panel.SetActive(true);
        }
    }
}
