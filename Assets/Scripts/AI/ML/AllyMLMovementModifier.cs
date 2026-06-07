using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ML-driven combat movement: optimal distance ring, strafing, and dodge offsets.
/// When active, Navigate/Retreat behavior nodes defer to this component.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AllyAIBrain))]
[RequireComponent(typeof(AllyMLBridge))]
public class AllyMLMovementModifier : MonoBehaviour
{
    [SerializeField] private AllyAIBrain _brain;
    [SerializeField] private AllyMLBridge _mlBridge;
    [SerializeField] private ThreatPerception _threatPerception;
    [SerializeField] private AllyDodgeHeuristic _dodgeHeuristic;
    [SerializeField] private float _strafeRadius = 2.5f;
    [SerializeField] private float _distanceTolerance = 0.35f;
    [SerializeField] private float _destinationStep = 1.5f;
    [SerializeField] private float _navSampleRadius = 2f;
    [SerializeField] private float _destinationRefreshInterval = 0.15f;

    private NavMeshAgent _navMeshAgent;
    private float _refreshTimer;
    private Vector3 _lastDestination;

    public bool IsControllingMovement { get; private set; }

    private void Awake()
    {
        if (_brain == null)
            _brain = GetComponent<AllyAIBrain>();
        if (_mlBridge == null)
            _mlBridge = GetComponent<AllyMLBridge>();
        if (_threatPerception == null)
            _threatPerception = GetComponent<ThreatPerception>();
        if (_dodgeHeuristic == null)
            _dodgeHeuristic = GetComponent<AllyDodgeHeuristic>();

        EnsureComponent(ref _threatPerception);
        EnsureComponent(ref _dodgeHeuristic);

        _navMeshAgent = GetComponentInChildren<NavMeshAgent>();
    }

    private void EnsureComponent<T>(ref T component) where T : Component
    {
        if (component != null)
            return;

        component = GetComponent<T>();
        if (component == null)
            component = gameObject.AddComponent<T>();
    }

    private void Update()
    {
        if (!ShouldControlMovement())
        {
            IsControllingMovement = false;
            return;
        }

        IsControllingMovement = true;
        _refreshTimer -= Time.deltaTime;
        if (_refreshTimer > 0f)
            return;

        _refreshTimer = _destinationRefreshInterval;
        ApplyCombatDestination();
    }

    public bool ShouldControlMovement()
    {
        if (_mlBridge == null || _brain == null)
            return false;

        if (!_mlBridge.IsMLActive || !_mlBridge.UseMLCombatMovement)
            return false;

        CombatSnapshot snapshot = _brain.Snapshot;
        return snapshot.EnemyCount > 0 && _brain.CurrentWeaponTarget != null;
    }

    public Vector3 ComputeDesiredWorldPosition()
    {
        Transform target = _brain.CurrentWeaponTarget;
        if (target == null)
            return transform.position;

        CombatSnapshot combat = _brain.Snapshot;
        ThreatSnapshot threat = _threatPerception != null
            ? _threatPerception.BuildSnapshot(transform.position, combat)
            : default;

        Vector2 allyPos = transform.position;
        Vector2 enemyPos = target.position;
        Vector2 toEnemy = enemyPos - allyPos;
        float currentDist = toEnemy.magnitude;

        if (currentDist < 0.01f)
            toEnemy = Vector2.right;
        else
            toEnemy /= currentDist;

        float targetDist = Mathf.Max(_brain.AttackDistance, 0.5f);
        Vector2 ringPoint = enemyPos - toEnemy * targetDist;

        Vector2 perp = new Vector2(-toEnemy.y, toEnemy.x);
        float strafeDir = _mlBridge.GetStrafeDirectionSigned();
        float strafeIntensity = _mlBridge.StrafeIntensity;
        Vector2 strafeOffset = perp * (strafeDir * strafeIntensity * _strafeRadius);

        Vector2 desired = ringPoint + strafeOffset;

        float distError = currentDist - targetDist;
        if (distError < -_distanceTolerance)
        {
            float retreatBlend = Mathf.Clamp01(_mlBridge.RetreatUrgency + 0.25f);
            desired = allyPos - toEnemy * (_destinationStep * retreatBlend);
        }
        else if (distError > _distanceTolerance)
        {
            desired = Vector2.Lerp(allyPos, ringPoint, 0.65f);
        }

        if (_dodgeHeuristic != null)
            desired += _dodgeHeuristic.ComputeDodgeOffset(transform.position, threat);

        if (_mlBridge.ShouldForceRetreat)
            desired = allyPos - toEnemy * _destinationStep;

        return new Vector3(desired.x, desired.y, transform.position.z);
    }

    private void ApplyCombatDestination()
    {
        if (_navMeshAgent == null || !_navMeshAgent.isOnNavMesh)
            return;

        Vector3 desired = ComputeDesiredWorldPosition();
        if (!TrySampleNavMesh(desired, out Vector3 valid))
            return;

        if (Vector3.Distance(valid, _lastDestination) < 0.05f)
            return;

        _lastDestination = valid;
        _navMeshAgent.isStopped = false;
        _navMeshAgent.SetDestination(valid);
    }

    private bool TrySampleNavMesh(Vector3 point, out Vector3 valid)
    {
        valid = point;
        if (!NavMesh.SamplePosition(point, out NavMeshHit hit, _navSampleRadius, NavMesh.AllAreas))
            return false;

        valid = hit.position;
        return true;
    }
}
