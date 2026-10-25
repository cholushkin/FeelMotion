using System.Collections.Generic;
using FeelMotion;
using LitMotion;
using UnityEngine;

namespace FeelMotion.Samples
{
    public enum EntityType
    {
        Coin,
        Fish,
        Heart,
        EnemyFish,
    }

    /// Moves left towards the cat, consumed once close enough. EnemyFish is the exception: on consume
    /// it swaps to the dead-fish sprite and punches away instead of being destroyed immediately.
    public sealed class Entity : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _moveSpeed = 1.5f;
        [SerializeField, Min(0f)] private float _consumeDistance = 0.5f;
        [SerializeField, Min(0f)] private float _despawnMarginX = 2f;
        [SerializeField] private FeelMotionPlayer _consumeMotionPlayer;

        EntityType _type;
        Transform _catTransform;
        Sprite _deadFishSprite;
        SpriteRenderer _spriteRenderer;
        bool _isConsumed;

        void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// Called by EntitiesSpawner right after Instantiate — the spawner owns the prefab-to-type map.
        public void Initialize(EntityType type, Transform catTransform, Sprite deadFishSprite = null)
        {
            _type = type;
            _catTransform = catTransform;
            _deadFishSprite = deadFishSprite;
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
            _isConsumed = true;

            switch (_type)
            {
                case EntityType.Coin:
                    GameHud.Instance?.AddCoin();
                    Destroy(gameObject);
                    break;

                case EntityType.Fish:
                    GameHud.Instance?.AddFish();
                    Destroy(gameObject);
                    break;

                case EntityType.Heart:
                    GameHud.Instance?.AddHeart();
                    Destroy(gameObject);
                    break;

                case EntityType.EnemyFish:
                    GameHud.Instance?.LoseLife();
                    TurnIntoDeadFish();
                    break;
            }
        }

        void TurnIntoDeadFish()
        {
            if (_spriteRenderer != null && _deadFishSprite != null) _spriteRenderer.sprite = _deadFishSprite;

            // "Flies back with air" — reuse the same punch action CatRunner's own juice already uses,
            // just with a bigger, backward-and-up offset instead of a tiny camera/landing kick.
            var player = gameObject.AddComponent<FeelMotionPlayer>();
            player.SetActions(new List<FeelMotionAction>
            {
                new TransformPositionPunchAction().Configure(
                    transform, MotionValueMode.FromCurrentTo,
                    new SerializableMotionSettings<Vector3, PunchOptions>
                    {
                        EndValue = new Vector3(0.6f, 0.4f, 0f),
                        Duration = 0.6f,
                        Options = new PunchOptions { Frequency = 6, DampingRatio = 0.4f },
                    }),
            });
            player.Play();

            Destroy(gameObject, 1f);
        }
    }
}
