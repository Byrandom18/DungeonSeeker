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
    [SerializeField] private bool _logEpisodeSpawns = true;

    [Header("Curriculum")]
    [SerializeField] private bool _useEpisodeBasedCurriculum = true;
    [SerializeField] private int _episodesPerCurriculumLevel = 100;
    [SerializeField] private int _maxCurriculumLevel = 4;
    [SerializeField] private bool _useAcademyCurriculumFallback;

    private float _episodeTimer;
    private int _curriculumLevel;
    private int _episodeIndex;
    private readonly List<EnemyDamage> _activeEnemies = new List<EnemyDamage>();

    public int CurriculumLevel => _curriculumLevel;
    public int EpisodeIndex => _episodeIndex;
    public float EpisodeTimer => _episodeTimer;
    public int LastWaveEnemyCount { get; private set; }
    public string LastWaveComposition => _spawner != null ? _spawner.LastWaveCompositionLabel : string.Empty;

    private void Awake()
    {
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

    private void FixedUpdate()
    {
        if (_trainingAgent == null)
            return;

        _episodeTimer += Time.fixedDeltaTime;
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

        AdvanceCurriculumForEpisode();

        PartyCombatCoordinator.ClearAll();
        ClearEpisodeProjectiles();

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

    public void RefreshCurriculumFromAcademy()
    {
        int level = AllyTrainingCurriculumHook.ReadCurriculumLevel(_curriculumLevel);
        SetCurriculumLevel(Mathf.Min(level, _maxCurriculumLevel));
    }

    private void AdvanceCurriculumForEpisode()
    {
        _episodeIndex++;

        if (_useEpisodeBasedCurriculum)
        {
            int previousLevel = _curriculumLevel;
            int episodesPerLevel = Mathf.Max(1, _episodesPerCurriculumLevel);
            int level = _startingCurriculumLevel + (_episodeIndex - 1) / episodesPerLevel;
            SetCurriculumLevel(Mathf.Min(level, _maxCurriculumLevel));

            if (_logEpisodeSpawns && _curriculumLevel != previousLevel)
            {
                int nextChangeEpisode = (_curriculumLevel - _startingCurriculumLevel + 1) * episodesPerLevel + 1;
                Debug.Log(
                    $"[AllyTraining] Curriculum level: {previousLevel} -> {_curriculumLevel} " +
                    $"(episode {_episodeIndex}, next at episode {nextChangeEpisode})");
            }

            return;
        }

        if (_useAcademyCurriculumFallback)
            RefreshCurriculumFromAcademy();
    }

    public static int GetEnemyCountForLevel(int level)
    {
        return AllyTrainingWaveComposition.GetTotalCount(level);
    }

    private static void ClearEpisodeProjectiles()
    {
        Projectile[] projectiles = FindObjectsByType<Projectile>(FindObjectsSortMode.None);
        for (int i = 0; i < projectiles.Length; i++)
        {
            if (projectiles[i] != null)
                Destroy(projectiles[i].gameObject);
        }

        EnemyProjectileRegistry.Clear();
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

        LastWaveEnemyCount = spawned.Count;
        _activeEnemies.Clear();
        _activeEnemies.AddRange(spawned);

        foreach (EnemyDamage enemy in spawned)
        {
            if (enemy == null) continue;
            enemy.OnDeath += (_, __) => HandleEnemyDeath(enemy);
        }

        if (_logEpisodeSpawns)
        {
            Debug.Log(
                $"[AllyTraining] Episode spawn: curriculum={_curriculumLevel}, " +
                $"enemies={LastWaveEnemyCount}, wave={LastWaveComposition}");
        }
    }

    private void HandleEnemyDeath(EnemyDamage enemy)
    {
        if (enemy == null) return;
        NotifyEnemyKilled(enemy, enemy.LastAttacker);
    }
}
