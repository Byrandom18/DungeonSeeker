using UnityEngine;

/// <summary>
/// Receives ML-Agents outputs and exposes combat pacing values to Behavior / AllyAIBrain.
/// </summary>
[DisallowMultipleComponent]
public class AllyMLBridge : MonoBehaviour
{
    [SerializeField] private AllyAIBrain _brain;
    [SerializeField] private bool _useMLDistance = true;
    [SerializeField] private bool _useMLRetreat = true;

    [Range(0f, 1f)] [SerializeField] private float _preferredDistance = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float _retreatUrgency;

    public float PreferredDistance => _preferredDistance;
    public float RetreatUrgency => _retreatUrgency;
    public bool ShouldForceRetreat => _useMLRetreat && _retreatUrgency >= 0.75f;

    private void Awake()
    {
        if (_brain == null)
            _brain = GetComponent<AllyAIBrain>();
    }

    public void SetMLActions(float preferredDistance, float retreatUrgency)
    {
        _preferredDistance = Mathf.Clamp01(preferredDistance);
        _retreatUrgency = Mathf.Clamp01(retreatUrgency);
    }

    public float GetCombatDistanceScale()
    {
        if (!_useMLDistance)
            return 1f;

        return Mathf.Lerp(0.5f, 1.3f, _preferredDistance);
    }

    public void ApplyProfileDefaults(AllyAIProfile profile)
    {
        if (profile == null) return;
        _preferredDistance = profile.DefaultPreferredDistance;
        _retreatUrgency = profile.DefaultRetreatUrgency;
    }
}
