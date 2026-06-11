using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Spawns enemy prefabs at training spawn points.
/// </summary>
public class AllyTrainingEnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] _enemyPrefabs;
    [SerializeField] private float _spawnRadius = 0.5f;

    private readonly List<EnemyDamage> _spawned = new List<EnemyDamage>();
    private readonly StringBuilder _waveLogBuilder = new StringBuilder(64);

    public IReadOnlyList<EnemyDamage> Spawned => _spawned;
    public string LastWaveCompositionLabel { get; private set; } = string.Empty;

    public List<EnemyDamage> SpawnEnemies(Transform[] spawnPoints, int count, int curriculumLevel)
    {
        ClearSpawned();

        if (_enemyPrefabs == null || _enemyPrefabs.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
            return _spawned;

        _waveLogBuilder.Clear();
        var prefabUseCounts = new int[_enemyPrefabs.Length];

        for (int i = 0; i < count; i++)
        {
            Transform point = spawnPoints[i % spawnPoints.Length];
            Vector2 offset = Random.insideUnitCircle * _spawnRadius;
            Vector3 pos = point.position + new Vector3(offset.x, offset.y, 0f);

            int prefabIndex = AllyTrainingWaveComposition.GetPrefabIndex(
                curriculumLevel, i, count, _enemyPrefabs.Length);
            GameObject prefab = _enemyPrefabs[prefabIndex];
            if (prefab == null)
                continue;

            prefabUseCounts[prefabIndex]++;

            GameObject instance = Instantiate(prefab, pos, Quaternion.identity, transform);
            if (!instance.TryGetComponent(out EnemyDamage enemy))
            {
                Destroy(instance);
                continue;
            }

            enemy.ResetForTraining();
            _spawned.Add(enemy);
        }

        LastWaveCompositionLabel = BuildWaveLabel(prefabUseCounts);
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
        LastWaveCompositionLabel = string.Empty;
    }

    private string BuildWaveLabel(int[] prefabUseCounts)
    {
        _waveLogBuilder.Clear();
        for (int i = 0; i < prefabUseCounts.Length; i++)
        {
            if (prefabUseCounts[i] <= 0)
                continue;

            if (_waveLogBuilder.Length > 0)
                _waveLogBuilder.Append(" + ");

            string label = _enemyPrefabs[i] != null ? _enemyPrefabs[i].name : $"prefab{i}";
            _waveLogBuilder.Append(prefabUseCounts[i]).Append('x').Append(label);
        }

        return _waveLogBuilder.Length > 0 ? _waveLogBuilder.ToString() : "none";
    }
}
