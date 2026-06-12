using Unity.MLAgents;
using UnityEngine;

/// <summary>
/// Optional: reads curriculum_level from Python ML-Agents config.
/// By default <see cref="AllyTrainingEnvironment"/> advances curriculum every N episodes locally.
/// Enable only when AllyTrainingEnvironment.UseAcademyCurriculumFallback is true.
/// </summary>
public class AllyTrainingCurriculumHook : MonoBehaviour
{
    public const string ParameterName = "curriculum_level";

    [SerializeField] private AllyTrainingEnvironment _environment;
    [SerializeField] private bool _syncFromAcademy;
    [SerializeField] private bool _logLevelChanges = true;

    private int _lastAppliedLevel = int.MinValue;

    private void Awake()
    {
        if (_environment == null)
            _environment = GetComponent<AllyTrainingEnvironment>();
    }

    private void LateUpdate()
    {
        if (!_syncFromAcademy)
            return;

        ApplyCurriculumFromAcademy();
    }

    public static int ReadCurriculumLevel(int fallback = 0)
    {
        if (Academy.Instance == null)
            return fallback;

        return (int)Academy.Instance.EnvironmentParameters.GetWithDefault(ParameterName, fallback);
    }

    private void ApplyCurriculumFromAcademy()
    {
        if (_environment == null)
            return;

        int level = ReadCurriculumLevel(_environment.CurriculumLevel);
        if (level == _lastAppliedLevel)
            return;

        _environment.SetCurriculumLevel(level);

        if (_logLevelChanges && _lastAppliedLevel != int.MinValue)
        {
            Debug.Log(
                $"[AllyTraining] Curriculum level changed: {_lastAppliedLevel} -> {level} " +
                $" (enemies={AllyTrainingEnvironment.GetEnemyCountForLevel(level)})");
        }

        _lastAppliedLevel = level;
    }
}
