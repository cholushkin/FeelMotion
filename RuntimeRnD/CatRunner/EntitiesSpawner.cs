using UnityEngine;

// TODO: spawn entities within the radius using parameters from the inspector
// TODO: draw gizmos radius and some most important text values 
// TODO: spawns coins, enemy fish, fish, hearts with different rates


namespace FeelMotion.Samples
{
    public sealed class EntitiesSpawner : MonoBehaviour
    {
        [SerializeField] private float _spawnerRadius = 1f;
        [SerializeField] private Entity _entityEnemyFishPrefab;
        [SerializeField] private Entity _entityDeadEnemyFishPrefab;
        [SerializeField] private Entity _entityCoinPrefab;
        [SerializeField] private Entity _entityFishPrefab;
        [SerializeField] private Entity _entityHeartPrefab;
        
    }
}
