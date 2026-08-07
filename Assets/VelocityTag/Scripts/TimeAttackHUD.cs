// TimeAttackHUD.cs
// Minimal in-round HUD: score / timer / combo readouts, countdown + GO banner,
// last-award breakdown line, and a crosshair tinted by TagBlaster's reticle
// state (Ready green / Cooldown red-purple / TargetLock gold, per combat.js).
// Pure presentation: reads TimeAttackRound public props and GameEventBus
// signals only — no gameplay state lives here.

using UnityEngine;
using UnityEngine.UI;
using ShooterCore;

namespace VelocityTag
{
    public class TimeAttackHUD : MonoBehaviour
    {
        [SerializeField] private TimeAttackRound _round;

        [Header("Widgets")]
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _timerText;
        [SerializeField] private Text _comboText;
        [SerializeField] private Text _centerText;     // countdown digits / GO!
        [SerializeField] private Text _breakdownText;  // e.g. "HELMET +150 AIR +50 x3"
        [SerializeField] private Image _crosshair;

        [Header("Reticle colors")]
        [SerializeField] private Color _readyColor = new Color(0.2f, 1f, 0.4f);
        [SerializeField] private Color _cooldownColor = new Color(0.85f, 0.2f, 0.6f);
        [SerializeField] private Color _lockColor = new Color(1f, 0.85f, 0.2f);

        private float _centerClearAt = -1f;
        private float _breakdownClearAt = -1f;

        private void OnEnable()
        {
            GameEventBus.On<TimeAttackCountdownTickEvent>(OnCountdownTick);
            GameEventBus.On<TimeAttackStartEvent>(OnRoundStart);
            GameEventBus.On<ScoreAwardedEvent>(OnScoreAwarded);
            GameEventBus.On<ComboDroppedEvent>(OnComboDropped);
            GameEventBus.On<ReticleStateChangedEvent>(OnReticleChanged);
            GameEventBus.On<RunEndedEvent>(OnRunEnded);
        }

        private void OnDisable()
        {
            GameEventBus.Off<TimeAttackCountdownTickEvent>(OnCountdownTick);
            GameEventBus.Off<TimeAttackStartEvent>(OnRoundStart);
            GameEventBus.Off<ScoreAwardedEvent>(OnScoreAwarded);
            GameEventBus.Off<ComboDroppedEvent>(OnComboDropped);
            GameEventBus.Off<ReticleStateChangedEvent>(OnReticleChanged);
            GameEventBus.Off<RunEndedEvent>(OnRunEnded);
        }

        private void Start()
        {
            if (_crosshair != null) _crosshair.color = _readyColor;
        }

        private void Update()
        {
            if (_round != null)
            {
                if (_scoreText != null) _scoreText.text = _round.Score.ToString("N0");
                if (_timerText != null)
                {
                    int t = Mathf.CeilToInt(Mathf.Max(0f, _round.Timer));
                    _timerText.text = (t / 60) + ":" + (t % 60).ToString("00");
                }
                if (_comboText != null && _round.Combo <= 1 && _comboText.text.Length > 0)
                    _comboText.text = "";
                if (_comboText != null && _round.Combo > 1)
                    _comboText.text = "x" + _round.Combo;
            }

            if (_centerClearAt > 0f && Time.time >= _centerClearAt)
            {
                _centerClearAt = -1f;
                if (_centerText != null) _centerText.text = "";
            }
            if (_breakdownClearAt > 0f && Time.time >= _breakdownClearAt)
            {
                _breakdownClearAt = -1f;
                if (_breakdownText != null) _breakdownText.text = "";
            }
        }

        private void OnCountdownTick(TimeAttackCountdownTickEvent e)
        {
            if (_centerText != null && e.SecondsLeft > 0)
            {
                _centerText.text = e.SecondsLeft.ToString();
                _centerText.color = new Color(1f, 0.84f, 0.42f); // hud.js #ffd76a
            }
            _centerClearAt = -1f;
        }

        private void OnRoundStart(TimeAttackStartEvent _)
        {
            if (_centerText != null)
            {
                _centerText.text = "GO!";
                _centerText.color = new Color(0f, 1f, 0.4f); // hud.js #00ff66
            }
            _centerClearAt = Time.time + 0.75f;
        }

        private void OnScoreAwarded(ScoreAwardedEvent e)
        {
            if (_breakdownText != null) _breakdownText.text = e.BreakdownText;
            _breakdownClearAt = Time.time + 1.5f;
        }

        private void OnComboDropped(ComboDroppedEvent _)
        {
            if (_comboText != null) _comboText.text = "";
        }

        private void OnReticleChanged(ReticleStateChangedEvent e)
        {
            if (_crosshair == null) return;
            if (e.State == ReticleState.Cooldown) _crosshair.color = _cooldownColor;
            else if (e.State == ReticleState.TargetLock) _crosshair.color = _lockColor;
            else _crosshair.color = _readyColor;
        }

        private void OnRunEnded(RunEndedEvent _)
        {
            if (_centerText != null) _centerText.text = "";
            if (_breakdownText != null) _breakdownText.text = "";
        }
    }
}
