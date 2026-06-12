using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ML-driven combat movement: optimal distance ring, strafing, dodge offsets,
/// low-health escape toward allies, and catch-up movement toward the party leader.
/// Driven by <see cref="AllyMLCombatMovementAction"/> when the behavior graph is active.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AllyAIBrain))]
[RequireComponent(typeof(AllyMLBridge))]
public class AllyMLMovementModifier : MonoBehaviour
{
    [Header("Combat ring")]
    [SerializeField] private float _strafeRadius = 2.5f;
    [SerializeField] private float _distanceTolerance = 0.35f;
    [SerializeField] private float _destinationStep = 1.5f;

    [Header("Low-health escape")]
    [SerializeField] private float _lowHealthHysteresis = 30f;
    [SerializeField] private float _allyHoldRadius = 2.5f;
    [SerializeField] private float _maxEscapeDistanceFromParty = 5f;
    [SerializeField] private float _enemyThreatRadiusDuringEscape = 3.5f;
    [SerializeField] private float _escapeApproachStep = 1.2f;
    [SerializeField] private float _holdRetreatStep = 0.75f;

    [Header("Follow leader")]
    [SerializeField] private float _followLeaderStep = 2f;

    [Header("Navigation")]
    [SerializeField] private float _navSampleRadius = 2f;
    [SerializeField] private float _destinationRefreshInterval = 0.15f;

    [SerializeField] private AllyAIBrain _brain;
    [SerializeField] private AllyMLBridge _mlBridge;
    [SerializeField] private ThreatPerception _threatPerception;
    [SerializeField] private AllyDodgeHeuristic _dodgeHeuristic;

    private NavMeshAgent _navMeshAgent;
    private float _refreshTimer;
    private Vector3 _lastDestination;
    private bool _graphDriving;
    private bool _isInLowHealthEscape;

    private float _healthPercent = 100f;
    private float _lowHealthThreshold = 20f;
    private float _maxDistToLeader = 10f;

    public bool IsControllingMovement { get; private set; }
    public bool IsInLowHealthEscape => _isInLowHealthEscape;
    public float LowHealthExitThreshold => _lowHealthThreshold + _lowHealthHysteresis;
    public AllyMLMovementMode CurrentMode { get; private set; }

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

    private void FixedUpdate()
    {
        if (!ShouldControlMovement())
        {
            IsControllingMovement = false;
            return;
        }

        IsControllingMovement = true;
        _refreshTimer -= Time.fixedDeltaTime;
        if (_refreshTimer > 0f)
            return;

        _refreshTimer = _destinationRefreshInterval;
        ApplyCombatDestination();
    }

    public void SetGraphDriving(bool active)
    {
        _graphDriving = active;
        if (!active)
        {
            IsControllingMovement = false;
            _isInLowHealthEscape = false;
        }
    }

    public void SetMovementContext(
        float healthPercent,
        float lowHealthThreshold,
        float maxDistToLeader,
        float lowHealthHysteresis = -1f)
    {
        _healthPercent = healthPercent;
        _lowHealthThreshold = lowHealthThreshold;
        _maxDistToLeader = maxDistToLeader;

        if (lowHealthHysteresis >= 0f)
            _lowHealthHysteresis = lowHealthHysteresis;
    }

    public bool ShouldControlMovement()
    {
        if (_mlBridge == null || _brain == null)
            return false;

        if (!_mlBridge.IsMLActive || !_mlBridge.UseMLCombatMovement)
            return false;

        if (_graphDriving)
        {
            return EvaluateMovementMode() switch
            {
                AllyMLMovementMode.EscapeToAlly => true,
                AllyMLMovementMode.FollowLeader => GetLeaderDistance() > _maxDistToLeader,
                _ => HasCombatMovementTarget()
            };
        }

        return HasCombatMovementTarget();
    }

    public Vector3 ComputeDesiredWorldPosition()
    {
        CombatSnapshot combat = _brain.Snapshot;
        ThreatSnapshot threat = _threatPerception != null
            ? _threatPerception.BuildSnapshot(transform.position, combat)
            : default;

        CurrentMode = EvaluateMovementMode();
        Vector2 desired = CurrentMode switch
        {
            AllyMLMovementMode.EscapeToAlly => ComputeEscapePosition(combat),
            AllyMLMovementMode.FollowLeader => ComputeFollowLeaderPosition(),
            _ => ComputeCombatPosition(combat)
        };

        desired = ApplyThreatMitigation(desired, threat);
        return new Vector3(desired.x, desired.y, transform.position.z);
    }

    private AllyMLMovementMode EvaluateMovementMode()
    {
        UpdateLowHealthEscapeState();

        if (_isInLowHealthEscape)
            return AllyMLMovementMode.EscapeToAlly;

        if (GetLeaderDistance() > _maxDistToLeader)
            return AllyMLMovementMode.FollowLeader;

        if (HasCombatMovementTarget())
            return AllyMLMovementMode.CombatPositioning;

        return AllyMLMovementMode.FollowLeader;
    }

    private void UpdateLowHealthEscapeState()
    {
        if (!HasCombatMovementTarget())
        {
            _isInLowHealthEscape = false;
            return;
        }

        float exitThreshold = _lowHealthThreshold + _lowHealthHysteresis;

        if (!_isInLowHealthEscape && _healthPercent <= _lowHealthThreshold)
            _isInLowHealthEscape = true;

        if (_isInLowHealthEscape && _healthPercent >= exitThreshold)
            _isInLowHealthEscape = false;
    }

    private bool HasCombatMovementTarget()
    {
        CombatSnapshot snapshot = _brain.Snapshot;
        return snapshot.EnemyCount > 0 && _brain.CurrentWeaponTarget != null;
    }

    private Vector2 ComputeCombatPosition(CombatSnapshot combat)
    {
        Transform target = _brain.CurrentWeaponTarget;
        if (target == null)
            return transform.position;

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

        if (_mlBridge.ShouldForceRetreat)
            desired = allyPos - toEnemy * _destinationStep;

        return desired;
    }

    private Vector2 ComputeEscapePosition(CombatSnapshot combat)
    {
        Vector2 allyPos = transform.position;
        if (!TryGetPartyAnchor(allyPos, out Vector2 anchor, out float distToAnchor))
            return ComputeFallbackEscape(allyPos);

        if (distToAnchor > _maxEscapeDistanceFromParty)
            return MoveToward(allyPos, anchor, _escapeApproachStep * 1.25f);

        Transform enemy = _brain.CurrentWeaponTarget;
        float enemyDist = enemy != null
            ? Vector2.Distance(allyPos, enemy.position)
            : float.MaxValue;

        if (distToAnchor > _allyHoldRadius)
        {
            Vector2 holdPoint = GetHoldPointNearAnchor(anchor, enemy, allyPos);
            float step = Mathf.Lerp(_escapeApproachStep, _escapeApproachStep * 0.5f,
                1f - Mathf.Clamp01(distToAnchor / _maxEscapeDistanceFromParty));
            return MoveToward(allyPos, holdPoint, step);
        }

        if (enemy != null && enemyDist < _enemyThreatRadiusDuringEscape)
        {
            Vector2 awayFromEnemy = allyPos - (Vector2)enemy.position;
            if (awayFromEnemy.sqrMagnitude < 0.0001f)
                awayFromEnemy = Vector2.right;
            awayFromEnemy.Normalize();

            float threatBlend = Mathf.InverseLerp(_allyHoldRadius, 0f, enemyDist);
            return allyPos + awayFromEnemy * (_holdRetreatStep * threatBlend);
        }

        Vector2 settlePoint = GetHoldPointNearAnchor(anchor, enemy, allyPos);
        if (Vector2.Distance(allyPos, settlePoint) > 0.35f)
            return MoveToward(allyPos, settlePoint, _holdRetreatStep * 0.5f);

        return allyPos;
    }

    private Vector2 ComputeFallbackEscape(Vector2 allyPos)
    {
        Transform enemy = _brain.CurrentWeaponTarget;
        if (enemy == null)
            return ComputeFollowLeaderPosition();
        Vector2 awayFromEnemy = allyPos - (Vector2)enemy.position;
        if (awayFromEnemy.sqrMagnitude < 0.0001f)
            awayFromEnemy = Vector2.right;

        return allyPos + awayFromEnemy.normalized * _holdRetreatStep;
    }

    private Vector2 GetHoldPointNearAnchor(Vector2 anchor, Transform enemy, Vector2 allyPos)
    {
        if (enemy == null)
            return anchor;

        Vector2 awayFromEnemy = anchor - (Vector2)enemy.position;
        if (awayFromEnemy.sqrMagnitude < 0.0001f)
            return anchor;

        awayFromEnemy.Normalize();
        float offset = _allyHoldRadius * 0.45f;
        Vector2 holdPoint = anchor + awayFromEnemy * offset;

        float distToHold = Vector2.Distance(allyPos, holdPoint);
        if (distToHold > _allyHoldRadius)
            holdPoint = Vector2.Lerp(anchor, holdPoint, _allyHoldRadius / distToHold);

        return holdPoint;
    }

    private static Vector2 MoveToward(Vector2 from, Vector2 to, float step)
    {
        return Vector2.MoveTowards(from, to, step);
    }

    private Vector2 ComputeFollowLeaderPosition()
    {
        Vector2 allyPos = transform.position;
        Transform leader = PartyManager.Instance?.LeaderTransform;

        if (leader == null)
            return allyPos;

        Vector2 leaderPos = leader.position;
        float dist = Vector2.Distance(allyPos, leaderPos);
        if (dist <= _maxDistToLeader * 0.85f)
            return allyPos;

        Vector2 toLeader = leaderPos - allyPos;
        if (toLeader.sqrMagnitude < 0.0001f)
            return allyPos;

        toLeader.Normalize();
        float catchUp = Mathf.InverseLerp(_maxDistToLeader, _maxDistToLeader * 1.75f, dist);
        return allyPos + toLeader * (_followLeaderStep * Mathf.Clamp(catchUp, 0.35f, 1.25f));
    }

    private bool TryGetPartyAnchor(Vector2 allyPos, out Vector2 anchor, out float distToAnchor)
    {
        anchor = allyPos;
        distToAnchor = 0f;

        Transform leader = PartyManager.Instance?.LeaderTransform;
        if (leader != null)
        {
            anchor = leader.position;
            distToAnchor = Vector2.Distance(allyPos, anchor);
            return true;
        }

        if (PartyManager.Instance == null)
            return false;

        ICharacterEntity nearest = null;
        float minDistSq = float.MaxValue;

        foreach (ICharacterEntity member in PartyManager.Instance.Members)
        {
            if (member == null || !member.IsAlive)
                continue;
            if (member.Transform == transform)
                continue;

            float sq = ((Vector2)member.Transform.position - allyPos).sqrMagnitude;
            if (sq < minDistSq)
            {
                minDistSq = sq;
                nearest = member;
            }
        }

        if (nearest == null)
            return false;

        anchor = nearest.Transform.position;
        distToAnchor = Mathf.Sqrt(minDistSq);
        return true;
    }

    private Vector2 ApplyThreatMitigation(Vector2 desired, ThreatSnapshot threat)
    {
        if (_dodgeHeuristic == null)
            return desired;

        Vector2 dodge = _dodgeHeuristic.ComputeDodgeOffset(transform.position, threat);
        if (CurrentMode == AllyMLMovementMode.CombatPositioning && _mlBridge.StrafeIntensity > 0.05f)
            dodge *= 0.85f;

        return desired + dodge;
    }

    private float GetLeaderDistance()
    {
        Transform leader = PartyManager.Instance?.LeaderTransform;
        if (leader == null)
            return 0f;

        return Vector2.Distance(transform.position, leader.position);
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
