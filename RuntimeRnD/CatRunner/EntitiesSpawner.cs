using UnityEngine;

namespace FeelMotion.Samples
{
    /// Auto-spawns entities at their own rate; the HUD's buttons can force an extra spawn of a specific
    /// kind. Spawn position is randomized within _spawnerRadius so entities don't all enter on the
    /// same line.
    public sealed class EntitiesSpawner : MonoBehaviour
    {
        enum SpawnKind
        {
            Coin,
            Fish,
            Heart,
            EnemyFish,
        }

        [SerializeField] private float _spawnerRadius = 1f;
        [SerializeField] private EnemyEntity _entityEnemyFishPrefab;
        [SerializeField] private PickableEntity _entityCoinPrefab;
        [SerializeField] private PickableEntity _entityFishPrefab;
        [SerializeField] private PickableEntity _entityHeartPrefab;

        [SerializeField] private Transform _catTransform;

        [SerializeField, Min(0f)] private float _coinSpawnInterval = 2.5f;
        [SerializeField, Min(0f)] private float _fishSpawnInterval = 2f;
        [SerializeField, Min(0f)] private float _heartSpawnInterval = 6f;
        [SerializeField, Min(0f)] private float _enemyFishSpawnInterval = 3.5f;

        float _coinTimer;
        float _fishTimer;
        float _heartTimer;
        float _enemyFishTimer;

        bool _isSpawningEnabled = true;

        void Update()
        {
            if (!_isSpawningEnabled) return;

            Tick(ref _coinTimer, _coinSpawnInterval, SpawnKind.Coin);
            Tick(ref _fishTimer, _fishSpawnInterval, SpawnKind.Fish);
            Tick(ref _heartTimer, _heartSpawnInterval, SpawnKind.Heart);
            Tick(ref _enemyFishTimer, _enemyFishSpawnInterval, SpawnKind.EnemyFish);
        }

        void Tick(ref float timer, float interval, SpawnKind kind)
        {
            timer += Time.deltaTime;
            if (timer < interval) return;

            timer = 0f;
            SpawnEntity(kind);
        }

        /// Called by the HUD once lives run out — forced spawns from the buttons still work.
        public void StopSpawning() => _isSpawningEnabled = false;

        public void ForceSpawnCoin() => SpawnEntity(SpawnKind.Coin);
        public void ForceSpawnFish() => SpawnEntity(SpawnKind.Fish);
        public void ForceSpawnHeart() => SpawnEntity(SpawnKind.Heart);

        void SpawnEntity(SpawnKind kind)
        {
            var prefab = PrefabFor(kind);
            if (prefab == null) return;

            var spawnPosition = transform.position + (Vector3)(Random.insideUnitCircle * _spawnerRadius);
            var entity = Instantiate(prefab, spawnPosition, prefab.transform.rotation, transform.parent);
            entity.Initialize(_catTransform);
        }

        Entity PrefabFor(SpawnKind kind)
        {
            switch (kind)
            {
                case SpawnKind.Coin: return _entityCoinPrefab;
                case SpawnKind.Fish: return _entityFishPrefab;
                case SpawnKind.Heart: return _entityHeartPrefab;
                case SpawnKind.EnemyFish: return _entityEnemyFishPrefab;
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
