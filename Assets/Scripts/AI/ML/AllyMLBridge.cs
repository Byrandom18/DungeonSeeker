using UnityEngine;

/// <summary>
/// Receives ML-Agents outputs and exposes combat pacing values to Behavior / AllyAIBrain.
/// </summary>
[DisallowMultipleComponent]
public class AllyMLBridge : MonoBehaviour
{
    public const float ObservationCount = 17f;
    public const int ContinuousActionCount = 4;

    [SerializeField] private AllyAIBrain _brain;
    [SerializeField] private AllyMLMode _mode = AllyMLMode.Heuristic;
    [SerializeField] private bool _useMLDistance = true;
    [SerializeField] private bool _useMLRetreat = true;
    [SerializeField] private bool _useMLCombatMovement = true;

    [Range(0f, 1f)] [SerializeField] private float _preferredDistance = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float _retreatUrgency;
    [Range(0f, 1f)] [SerializeField] private float _strafeDirection = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float _strafeIntensity;

    public AllyMLMode Mode => _mode;
    public float PreferredDistance => _preferredDistance;
    public float RetreatUrgency => _retreatUrgency;
    public float StrafeDirection => _strafeDirection;
    public float StrafeIntensity => _strafeIntensity;
    public bool UseMLCombatMovement => _useMLCombatMovement;
    public bool IsMLActive => _mode is AllyMLMode.Training or AllyMLMode.Inference or AllyMLMode.Heuristic;
    public bool ShouldForceRetreat =>
        _useMLRetreat && _mode != AllyMLMode.Disabled && _retreatUrgency >= 0.75f;

    private void Awake()
    {
        if (_brain == null)
            _brain = GetComponent<AllyAIBrain>();
    }

    public void SetMode(AllyMLMode mode)
    {
        _mode = mode;
        ApplyModeDefaults();
    }

    public void SetMLActions(float preferredDistance, float retreatUrgency, float strafeDirection, float strafeIntensity)
    {
        if (_mode == AllyMLMode.Disabled)
            return;

        _preferredDistance = Mathf.Clamp01(preferredDistance);
        _retreatUrgency = Mathf.Clamp01(retreatUrgency);
        _strafeDirection = Mathf.Clamp01(strafeDirection);
        _strafeIntensity = Mathf.Clamp01(strafeIntensity);
    }

    public void SetMLActions(float preferredDistance, float retreatUrgency)
    {
        SetMLActions(preferredDistance, retreatUrgency, _strafeDirection, _strafeIntensity);
    }

    public float GetCombatDistanceScale()
    {
        if (!_useMLDistance || _mode == AllyMLMode.Disabled)
            return 1f;

        return Mathf.Lerp(0.5f, 1.3f, _preferredDistance);
    }

    /// <summary>Maps [0,1] ML output to [-1,1] lateral strafe direction.</summary>
    public float GetStrafeDirectionSigned()
    {
        return (_strafeDirection - 0.5f) * 2f;
    }

    public void ApplyProfileDefaults(AllyAIProfile profile)
    {
        if (profile == null) return;
        _preferredDistance = profile.DefaultPreferredDistance;
        _retreatUrgency = profile.DefaultRetreatUrgency;
    }

    private void ApplyModeDefaults()
    {
        switch (_mode)
        {
            case AllyMLMode.Disabled:
                _useMLDistance = false;
                _useMLRetreat = false;
                _useMLCombatMovement = false;
                _preferredDistance = 0.6f;
                _retreatUrgency = 0f;
                _strafeDirection = 0.5f;
                _strafeIntensity = 0f;
                break;
            case AllyMLMode.Heuristic:
                _useMLDistance = true;
                _useMLRetreat = true;
                _useMLCombatMovement = true;
                break;
            case AllyMLMode.Training:
            case AllyMLMode.Inference:
                _useMLDistance = true;
                _useMLRetreat = true;
                _useMLCombatMovement = true;
                break;
        }
    }
}
