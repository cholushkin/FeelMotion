using UnityEngine;

namespace FeelMotion.Samples
{
    /// EnemyFish — consuming it costs a life and swaps to the dead-fish sprite, then falls through to
    /// the base consume-motion playback (the fly-back/punch juice lives on _consumeMotionPlayer now,
    /// not hardcoded here).
    public sealed class EnemyEntity : Entity
    {
        [SerializeField] private Sprite _deadFishSprite;

        protected override void OnConsume()
        {
            GameHud.Instance?.LoseLife();

            if (SpriteRenderer != null && _deadFishSprite != null)
                SpriteRenderer.sprite = _deadFishSprite;

            base.OnConsume();
        }
    }
}
