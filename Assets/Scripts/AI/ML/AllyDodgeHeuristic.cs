using UnityEngine;

/// <summary>
/// Fast reactive dodge layer for incoming enemy projectiles.
/// Blends with ML strafe output in <see cref="AllyMLMovementModifier"/>.
/// </summary>
[DisallowMultipleComponent]
public class AllyDodgeHeuristic : MonoBehaviour
{
    [SerializeField] private ThreatPerception _threatPerception;
    [SerializeField] private float _urgencyThreshold = 0.45f;
    [SerializeField] private float _dodgeDistance = 1.4f;

    public Vector2 DodgeOffset { get; private set; }
    public bool IsDodging { get; private set; }

    private void Awake()
    {
        if (_threatPerception == null)
            _threatPerception = GetComponent<ThreatPerception>();
    }

    public Vector2 ComputeDodgeOffset(Vector3 position, ThreatSnapshot threat)
    {
        DodgeOffset = Vector2.zero;
        IsDodging = false;

        if (!threat.HasIncomingThreat || threat.IncomingThreatUrgency < _urgencyThreshold)
            return DodgeOffset;

        Vector2 threatDir = threat.ThreatDirection;
        if (threatDir.sqrMagnitude < 0.0001f)
            return DodgeOffset;

        Vector2 lateral = new Vector2(-threatDir.y, threatDir.x);
        float side = Random.value >= 0.5f ? 1f : -1f;
        lateral *= side;

        float strength = Mathf.InverseLerp(_urgencyThreshold, 1f, threat.IncomingThreatUrgency);
        DodgeOffset = lateral.normalized * (_dodgeDistance * strength);
        IsDodging = true;
        return DodgeOffset;
    }
}
