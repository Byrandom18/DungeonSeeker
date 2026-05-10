using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Ally navigation + basic attack loop. Expects the same building blocks as the player:
/// <see cref="PlayerStats"/> (with <c>_registerAsPrimaryPlayer = false</c>),
/// <see cref="EquipmentComponent"/> with a dedicated <see cref="InventorySO"/> asset,
/// own <see cref="ActiveWeapon"/> with <c>_registerPlayerSingleton = false</c>,
/// <see cref="AimMode"/> = AimAtWorldTarget or call <see cref="ActiveWeapon.SetWorldAimTarget"/>.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[DisallowMultipleComponent]
public class AllyAIController : MonoBehaviour
{
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private ActiveWeapon _activeWeapon;

    [Header("Nav")]
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private float _followRefresh = 0.25f;
    [SerializeField] private float _followStopDistance = 2f;
    [SerializeField] private float _followRadiusBuffer = 0.5f;

    [Header("Combat")]
    [SerializeField] private float _detectRadius = 12f;
    [SerializeField] private float _engageStopDistance = 1.75f;
    [SerializeField] private float _minAttackInterval = 0.35f;

    private readonly Collider2D[] _overlapBuffer = new Collider2D[32];

    private float _followTimer;
    private bool _canAttack = true;
    private Coroutine _attackRoutine;
    private Transform _enemyTarget;

    private void Awake()
    {
        if (_agent == null)
            _agent = GetComponent<NavMeshAgent>();

        _agent.updateRotation = false;
        _agent.updateUpAxis = false;

        if (_stats == null)
            _stats = GetComponent<PlayerStats>();

        if (_activeWeapon == null)
            _activeWeapon = GetComponentInChildren<ActiveWeapon>(true);
    }

    private void OnDisable()
    {
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }

        _canAttack = true;

        if (_activeWeapon != null)
        {
            _activeWeapon.SetWorldAimTarget(null);
            _activeWeapon.RotationEnabled = true;
        }

        if (_agent != null && _agent.isActiveAndEnabled)
            _agent.isStopped = true;
    }

    private void Update()
    {
        if (_stats == null || !_stats.IsAlive)
        {
            if (_agent != null && _agent.isActiveAndEnabled)
                _agent.isStopped = true;
            if (_activeWeapon != null)
                _activeWeapon.SetWorldAimTarget(null);
            return;
        }

        RefreshEnemyTarget();
        if (_activeWeapon != null)
            _activeWeapon.SetWorldAimTarget(_enemyTarget);

        if (_enemyTarget != null)
            MoveToEngage(_enemyTarget.position);
        else
            FollowPrimaryPlayer();

        if (_enemyTarget != null && _canAttack && _attackRoutine == null)
        {
            float dist = Vector2.Distance(transform.position, _enemyTarget.position);
            if (dist <= _engageStopDistance + _followRadiusBuffer)
                _attackRoutine = StartCoroutine(AttackRoutine());
        }
    }

    private void MoveToEngage(Vector3 world)
    {
        if (_agent == null || !_agent.isActiveAndEnabled) return;

        _agent.isStopped = false;
        if (NavMesh.SamplePosition(world, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
        else
            _agent.SetDestination(world);
    }

    private void FollowPrimaryPlayer()
    {
        if (_agent == null || !_agent.isActiveAndEnabled) return;

        _followTimer -= Time.deltaTime;
        if (_followTimer > 0f) return;
        _followTimer = _followRefresh;

        if (PlayerMovement.Instance == null)
        {
            _agent.isStopped = true;
            return;
        }

        Vector3 leader = PlayerMovement.Instance.transform.position;
        float dist = Vector2.Distance(transform.position, leader);

        if (dist > _followStopDistance)
        {
            _agent.isStopped = false;
            if (NavMesh.SamplePosition(leader, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                _agent.SetDestination(hit.position);
            else
                _agent.SetDestination(leader);
        }
        else
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }
    }

    private void RefreshEnemyTarget()
    {
        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position, _detectRadius, _overlapBuffer);

        Transform best = null;
        float bestSq = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider2D c = _overlapBuffer[i];
            if (c == null) continue;
            if (!c.TryGetComponent(out EnemyDamage ed) || !ed.IsAlive) continue;

            float sq = (c.transform.position - transform.position).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = c.transform;
            }
        }

        _enemyTarget = best;
    }

    private IEnumerator AttackRoutine()
    {
        _canAttack = false;

        if (_activeWeapon == null)
        {
            _canAttack = true;
            _attackRoutine = null;
            yield break;
        }

        WeaponBase weapon = _activeWeapon.GetActiveWeapon();
        if (weapon == null)
        {
            _canAttack = true;
            _attackRoutine = null;
            yield break;
        }

        bool lockRotation = weapon.WeaponData != null && weapon.WeaponData.LockRotationOnSwing;
        if (lockRotation)
            _activeWeapon.RotationEnabled = false;

        _activeWeapon.NotifyAttackStarted();
        weapon.Attack();

        float wait = Mathf.Max(weapon.Cooldown, _minAttackInterval);
        yield return new WaitForSeconds(wait);

        if (lockRotation)
            _activeWeapon.RotationEnabled = true;

        _activeWeapon.NotifyAttackEnded();

        _canAttack = true;
        _attackRoutine = null;
    }
}
