using Unity.MLAgents;
using UnityEngine;

/// <summary>
/// Reads curriculum_level from Python training config and applies it to the training environment.
/// Add to the same GameObject as AllyTrainingEnvironment.
/// </summary>
public class AllyTrainingCurriculumHook : MonoBehaviour
{
    [SerializeField] private AllyTrainingEnvironment _environment;

    private void Awake()
    {
        if (_environment == null)
            _environment = GetComponent<AllyTrainingEnvironment>();
    }

    private void Update()
    {
        if (_environment == null)
            return;

        int level = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("curriculum_level", 0f);
        _environment.SetCurriculumLevel(level);
    }
}
