using UnityEngine;

namespace FeelMotion.Samples
{
    /// Auto-spawns entities at their own rate; the HUD's buttons can force an extra spawn of a specific
    /// type. Spawn position is randomized within _spawnerRadius so entities don't all enter on the
    /// same line.
    public sealed class EntitiesSpawner : MonoBehaviour
    {
        [SerializeField] private float _spawnerRadius = 1f;
        [SerializeField] private Entity _entityEnemyFishPrefab;
        [SerializeField] private Entity _entityDeadEnemyFishPrefab;
        [SerializeField] private Entity _entityCoinPrefab;
        [SerializeField] private Entity _entityFishPrefab;
        [SerializeField] private Entity _entityHeartPrefab;

        [SerializeField] private Transform _catTransform;

        [SerializeField, Min(0f)] private float _coinSpawnInterval = 2.5f;
        [SerializeField, Min(0f)] private float _fishSpawnInterval = 2f;
        [SerializeField, Min(0f)] private float _heartSpawnInterval = 6f;
        [SerializeField, Min(0f)] private float _enemyFishSpawnInterval = 3.5f;

        float _coinTimer;
        float _fishTimer;
        float _heartTimer;
        float _enemyFishTimer;

        Sprite _deadEnemyFishSprite;
        bool _isSpawningEnabled = true;

        void Awake()
        {
            // EnemyDeadFish is never spawned on its own — Entity just borrows its sprite on hit.
            if (_entityDeadEnemyFishPrefab != null)
            {
                var deadSpriteRenderer = _entityDeadEnemyFishPrefab.GetComponent<SpriteRenderer>();
                if (deadSpriteRenderer != null) _deadEnemyFishSprite = deadSpriteRenderer.sprite;
            }
        }

        void Update()
        {
            if (!_isSpawningEnabled) return;

            Tick(ref _coinTimer, _coinSpawnInterval, EntityType.Coin);
            Tick(ref _fishTimer, _fishSpawnInterval, EntityType.Fish);
            Tick(ref _heartTimer, _heartSpawnInterval, EntityType.Heart);
            Tick(ref _enemyFishTimer, _enemyFishSpawnInterval, EntityType.EnemyFish);
        }

        void Tick(ref float timer, float interval, EntityType type)
        {
            timer += Time.deltaTime;
            if (timer < interval) return;

            timer = 0f;
            SpawnEntity(type);
        }

        /// Called by the HUD once lives run out — forced spawns from the buttons still work.
        public void StopSpawning() => _isSpawningEnabled = false;

        public void ForceSpawnCoin() => SpawnEntity(EntityType.Coin);
        public void ForceSpawnFish() => SpawnEntity(EntityType.Fish);
        public void ForceSpawnHeart() => SpawnEntity(EntityType.Heart);

        void SpawnEntity(EntityType type)
        {
            var prefab = PrefabFor(type);
            if (prefab == null) return;

            var spawnPosition = transform.position + (Vector3)(Random.insideUnitCircle * _spawnerRadius);
            var entity = Instantiate(prefab, spawnPosition, prefab.transform.rotation, transform.parent);
            entity.Initialize(type, _catTransform, _deadEnemyFishSprite);
        }

        Entity PrefabFor(EntityType type)
        {
            switch (type)
            {
                case EntityType.Coin: return _entityCoinPrefab;
                case EntityType.Fish: return _entityFishPrefab;
                case EntityType.Heart: return _entityHeartPrefab;
                case EntityType.EnemyFish: return _entityEnemyFishPrefab;
                default: return null;
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _spawnerRadius);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * (_spawnerRadius + 0.15f),
                $"Coin {_coinSpawnInterval}s | Fish {_fishSpawnInterval}s | Heart {_heartSpawnInterval}s | EnemyFish {_enemyFishSpawnInterval}s");
#endif
        }
    }
}
