using FeelMotion;
using UnityEngine;

namespace FeelMotion.Samples
{
    /// Moves left towards the cat, consumed once close enough. Subclasses implement their own
    /// reward/penalty in OnConsume() (score, life, sprite swap, ...); the shared consume-motion
    /// playback lives here so every entity behaves the same once consumed.
    public abstract class Entity : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _moveSpeed = 1.5f;
        [SerializeField, Min(0f)] private float _consumeDistance = 0.5f;
        [SerializeField, Min(0f)] private float _despawnMarginX = 2f;
        [SerializeField] private FeelMotionPlayer _consumeMotionPlayer;

        Transform _catTransform;
        SpriteRenderer _spriteRenderer;
        bool _isConsumed;

        protected SpriteRenderer SpriteRenderer => _spriteRenderer;

        protected virtual void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// Called by EntitiesSpawner right after Instantiate.
        public virtual void Initialize(Transform catTransform)
        {
            _catTransform = catTransform;
        }

        void Update()
        {
            if (_isConsumed || _catTransform == null) return;

            transform.position += Vector3.left * (_moveSpeed * Time.deltaTime);

            if (transform.position.x < _catTransform.position.x - _despawnMarginX)
            {
                Destroy(gameObject);
                return;
            }

            if (Vector3.Distance(transform.position, _catTransform.position) <= _consumeDistance)
                Consume();
        }

        void Consume()
        {
            if (_isConsumed) return;
            _isConsumed = true;

            OnConsume();
        }

        /// Entity-specific consume behavior (score/life changes, sprite swap, ...). Overrides must
        /// call base.OnConsume() last — it plays _consumeMotionPlayer (if assigned) and destroys the
        /// entity once playback completes, or destroys immediately when no consume player is set.
        protected virtual void OnConsume()
        {
            // todo: spawn a particle effect at transform.position once reward/death VFX are authored.

            if (_consumeMotionPlayer != null)
            {
                _consumeMotionPlayer.Completed += HandleConsumeMotionCompleted;
                _consumeMotionPlayer.Play();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void HandleConsumeMotionCompleted()
        {
            _consumeMotionPlayer.Completed -= HandleConsumeMotionCompleted;
            Destroy(gameObject);
        }
    }
}
