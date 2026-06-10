using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Training arena controller: episode reset, curriculum, enemy cleanup.
/// Place one instance in the ML_Training_Arena scene.
/// </summary>
public class AllyTrainingEnvironment : MonoBehaviour
{
    public static AllyTrainingEnvironment Instance { get; private set; }
    public static event Action<EnemyDamage, ICharacterEntity> OnEnemyKilled;

    [Header("Spawns")]
    [SerializeField] private Transform _allySpawn;
    [SerializeField] private Transform _leaderSpawn;
    [SerializeField] private Transform[] _enemySpawnPoints;

    [Header("References")]
    [SerializeField] private AllyTrainingEnemySpawner _spawner;
    [SerializeField] private AllyTrainingLeaderDummy _leaderDummy;
    [SerializeField] private AllyPositionAgent _trainingAgent;

    [Header("Episode")]
    [SerializeField] private float _maxEpisodeSeconds = 90f;
    [SerializeField] private int _startingCurriculumLevel;

    private float _episodeTimer;
    private int _curriculumLevel;
    private readonly List<EnemyDamage> _activeEnemies = new List<EnemyDamage>();

    public int CurriculumLevel => _curriculumLevel;
    public float EpisodeTimer => _episodeTimer;

    private void Awake()
    {
        //Application.runInBackground = true;

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _curriculumLevel = _startingCurriculumLevel;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (_trainingAgent == null)
            return;

        _episodeTimer += Time.deltaTime;
        if (_episodeTimer >= _maxEpisodeSeconds)
            _trainingAgent.ForceEndEpisode(false);
    }

    public void RegisterAgent(AllyPositionAgent agent)
    {
        _trainingAgent = agent;
    }

    public void ResetEpisode(AllyPositionAgent agent)
    {
        _trainingAgent = agent;
        _episodeTimer = 0f;

        PartyCombatCoordinator.ClearAll();
        EnemyProjectileRegistry.Clear();

        if (_leaderDummy != null)
            _leaderDummy.ResetToSpawn(_leaderSpawn);

        ResetAgent(agent);
        ClearEnemies();
        SpawnWaveForCurrentLevel();

        agent.GetComponent<AllyMLBridge>()?.SetMode(AllyMLMode.Training);
    }

    public void NotifyEnemyKilled(EnemyDamage enemy, ICharacterEntity killer)
    {
        OnEnemyKilled?.Invoke(enemy, killer);
        _activeEnemies.Remove(enemy);

        if (_activeEnemies.Count == 0 && _trainingAgent != null)
            _trainingAgent.ForceEndEpisode(true);
    }

    public void SetCurriculumLevel(int level)
    {
        _curriculumLevel = Mathf.Max(0, level);
    }

    private void ResetAgent(AllyPositionAgent agent)
    {
        if (agent == null) return;

        Transform spawn = _allySpawn != null ? _allySpawn : agent.transform;
        agent.transform.position = spawn.position;

        if (agent.TryGetComponent(out NavMeshAgent nav) && nav.isOnNavMesh)
        {
            nav.ResetPath();
            nav.Warp(spawn.position);
        }

        if (agent.TryGetComponent(out PlayerStats stats))
            stats.ResetForTraining(restoreAlive: true);

        if (agent.TryGetComponent(out AllyAIBrain brain))
            brain.RefreshSnapshot();
    }

    private void ClearEnemies()
    {
        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            if (_activeEnemies[i] != null)
                Destroy(_activeEnemies[i].gameObject);
        }

        _activeEnemies.Clear();
        _spawner?.ClearSpawned();
    }

    private void SpawnWaveForCurrentLevel()
    {
        if (_spawner == null || _enemySpawnPoints == null || _enemySpawnPoints.Length == 0)
            return;

        int enemyCount = GetEnemyCountForLevel(_curriculumLevel);
        var spawned = _spawner.SpawnEnemies(_enemySpawnPoints, enemyCount, _curriculumLevel);

        _activeEnemies.Clear();
        _activeEnemies.AddRange(spawned);

        foreach (EnemyDamage enemy in spawned)
        {
            if (enemy == null) continue;
            enemy.OnDeath += (_, __) => HandleEnemyDeath(enemy);
        }
    }

    private void HandleEnemyDeath(EnemyDamage enemy)
    {
        if (enemy == null) return;
        NotifyEnemyKilled(enemy, enemy.LastAttacker);
    }

    private static int GetEnemyCountForLevel(int level)
    {
        return level switch
        {
            0 => 1,
            1 => 2,
            2 => 3,
            3 => 4,
            _ => 5
        };
    }
}
