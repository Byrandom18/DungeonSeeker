using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns enemy prefabs at training spawn points.
/// </summary>
public class AllyTrainingEnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] _enemyPrefabs;
    [SerializeField] private float _spawnRadius = 0.5f;

    private readonly List<EnemyDamage> _spawned = new List<EnemyDamage>();

    public IReadOnlyList<EnemyDamage> Spawned => _spawned;

    public List<EnemyDamage> SpawnEnemies(Transform[] spawnPoints, int count, int curriculumLevel)
    {
        ClearSpawned();

        if (_enemyPrefabs == null || _enemyPrefabs.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
            return _spawned;

        for (int i = 0; i < count; i++)
        {
            Transform point = spawnPoints[i % spawnPoints.Length];
            Vector2 offset = Random.insideUnitCircle * _spawnRadius;
            Vector3 pos = point.position + new Vector3(offset.x, offset.y, 0f);

            GameObject prefab = PickPrefab(curriculumLevel);
            if (prefab == null) continue;

            GameObject instance = Instantiate(prefab, pos, Quaternion.identity, transform);
            if (!instance.TryGetComponent(out EnemyDamage enemy))
            {
                Destroy(instance);
                continue;
            }

            enemy.ResetForTraining();
            _spawned.Add(enemy);
        }

        return _spawned;
    }

    public void ClearSpawned()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            if (_spawned[i] != null)
                Destroy(_spawned[i].gameObject);
        }

        _spawned.Clear();
    }

    private GameObject PickPrefab(int curriculumLevel)
    {
        if (_enemyPrefabs.Length == 1)
            return _enemyPrefabs[0];

        int index = Mathf.Clamp(curriculumLevel, 0, _enemyPrefabs.Length - 1);
        return _enemyPrefabs[index];
    }
}
