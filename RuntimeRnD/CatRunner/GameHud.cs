using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FeelMotion.Samples
{
    /// Minimal score/lives/progress HUD for the CatRunner demo. A tiny self-contained sample, so a
    /// plain singleton is simpler here than wiring up VitalRouter.
    public sealed class GameHud : MonoBehaviour
    {
        public static GameHud Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI _coinAmountText;
        [SerializeField] private TextMeshProUGUI _fishAmountText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private Image _progressFiller;
        [SerializeField] private Image[] _heartIcons;
        [SerializeField] private EntitiesSpawner _spawner;

        [SerializeField, Min(1)] private int _maxLives = 3;
        [SerializeField, Min(0f)] private float _progressPerConsume = 0.25f;

        int _coins;
        int _fish;
        int _level = 1;
        float _progress;
        int _lives;

        void Awake()
        {
            Instance = this;
            _lives = _maxLives;

            RefreshHearts();
            RefreshAmounts();
            RefreshProgress();
        }

        public void AddCoin()
        {
            _coins++;
            RefreshAmounts();
            AddProgress();
        }

        public void AddFish()
        {
            _fish++;
            RefreshAmounts();
            AddProgress();
        }

        public void AddHeart()
        {
            if (_lives < _maxLives) _lives++;
            RefreshHearts();
            AddProgress();
        }

        public void LoseLife()
        {
            _lives = Mathf.Max(0, _lives - 1);
            RefreshHearts();

            if (_lives == 0 && _spawner != null) _spawner.StopSpawning();
        }

        void AddProgress()
        {
            _progress += _progressPerConsume;
            while (_progress >= 1f)
            {
                _progress -= 1f;
                _level++;
            }

            RefreshProgress();
        }

        void RefreshAmounts()
        {
            // Plain text set for now — swap for a LitMotion pop-tween here when we want the "gain" juice.
            if (_coinAmountText != null) _coinAmountText.text = _coins.ToString();
            if (_fishAmountText != null) _fishAmountText.text = _fish.ToString();
        }

        void RefreshProgress()
        {
            if (_progressFiller != null) _progressFiller.fillAmount = _progress;
            if (_levelText != null) _levelText.text = _level.ToString();
        }

        void RefreshHearts()
        {
            if (_heartIcons == null) return;
            for (var i = 0; i < _heartIcons.Length; i++)
                if (_heartIcons[i] != null) _heartIcons[i].enabled = i < _lives;
        }
    }
}
