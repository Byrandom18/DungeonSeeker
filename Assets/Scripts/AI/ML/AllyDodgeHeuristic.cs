using UnityEngine;

/// <summary>
/// Fast reactive dodge layer for incoming enemy attacks and projectiles.
/// Blends with ML strafe output in <see cref="AllyMLMovementModifier"/>.
/// </summary>
[DisallowMultipleComponent]
public class AllyDodgeHeuristic : MonoBehaviour
{
    [SerializeField] private ThreatPerception _threatPerception;
    [SerializeField] private float _urgencyThreshold = 0.35f;
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

        float urgency = Mathf.Max(threat.IncomingThreatUrgency, threat.AttackingSelfUrgency);
        if (urgency < _urgencyThreshold)
            return DodgeOffset;

        Vector2 escapeDir = threat.ThreatDirection;
        if (escapeDir.sqrMagnitude < 0.0001f && threat.AnyEnemyAttackingSelf)
        {
            escapeDir = Vector2.right;
        }

        if (escapeDir.sqrMagnitude < 0.0001f)
            return DodgeOffset;

        float strength = Mathf.InverseLerp(_urgencyThreshold, 1f, urgency);
        DodgeOffset = escapeDir.normalized * (_dodgeDistance * strength);
        IsDodging = true;
        return DodgeOffset;
    }
}
