using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using GameUtils;
using System;
using Unity.VisualScripting;

[SelectionBase]
public class EnemyAI : MonoBehaviour
{
    private EnemySO _enemySO;
    [SerializeField] private State _startingState;

    [Header("Roaming settings")]
    [SerializeField] private bool _enableRoam = true;
    [SerializeField] private float _roamSpeed = 1.5f;
    [SerializeField] private float _roamingDistanceMax = 7f;
    [SerializeField] private float _roamingDistanceMin = 1f;
    [SerializeField] private float _roamingTimerMax = 2f;
    [SerializeField] private float _idleDuration = 5f;
    [SerializeField] private bool _canChangeStartPos = true;

    [Header("Chase settings")]
    [SerializeField] private float _chasingSpeed = 2.5f;
    [SerializeField] private float _chasingDistance = 5f;
    //[SerializeField] private float _chasingAnimationSpeedMultiplier = 1.5f;
    [SerializeField] private bool _isChasingEnemy = true;

    [Header("Stepping chase")]
    [SerializeField] private bool _useSteppedChase = false;
    [SerializeField] private float _stepDistance = 1.5f;  
    [SerializeField] private float _stepSpread = 30f;   
    [SerializeField] private float _stepPauseDuration = 0.4f;  

    [Header("Attack settings")]
    [SerializeField] private bool _isAttackingEnemy = true;
    [SerializeField] private float _attackDistance = 1f;
    [SerializeField] private float _attackRate = 3f;

    [Header("Retreat after attack")]
    [SerializeField] private bool _retreatAfterAttack = false;
    [SerializeField] private float _retreatDistance = 3f;   // желаемая дистанция отступления
    [SerializeField] private float _retreatSpeed = 2f;
    [SerializeField] private float _minSafeDistance = 5f;

    private NavMeshAgent _navMeshAgent;
    private EnemyDamage _enemyDamage;

    [SerializeField] private State _state;

    private float _roamingTime;
    private Vector3 _roamPosition;
    private Vector3 _startingPosition; // walk around start coordinate
    private float _nextAttackTime = 0f;
    private bool _isIdle = false;
    private bool _isRoaming = false;

    // Stepped chase
    private bool _isStepPausing = false;
    private bool _stepInProgress = false;

    // Retreat
    private bool _isRetreating = false;

    private ICharacterEntity _currentTarget;

    public bool IsFacingRight = true;
    public bool IsAttacking = false;
    public ICharacterEntity CurrentTarget => _currentTarget;

    public bool IsAttackingEntity(ICharacterEntity entity)
    {
        if (!IsAttacking || entity == null || _currentTarget == null)
            return false;

        return _currentTarget.Transform == entity.Transform;
    }

    public event EventHandler OnEnemyAttack;
    public event EventHandler OnEnemyUpdateSpriteDirection;
    
    private enum State
    {
        Idle,
        Roaming,
        Chasing,
        Attacking,
        Retreating,
        Death
    }

    private void Awake()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _enemyDamage = GetComponent<EnemyDamage>();
        _navMeshAgent.updateRotation = false;
        _navMeshAgent.updateUpAxis = false;
        _state = _startingState;
    }

    private void Start()
    {
        _enemySO = _enemyDamage.GetEnemySO();
        if (_enemyDamage == null) Debug.LogError($"EnemyDamage missing on {gameObject.name}");
        if (_enemySO == null) Debug.LogError($"EnemySO missing on {gameObject.name}");
        InitializeStats();
        _startingPosition = transform.position;
        _navMeshAgent.speed = _roamSpeed;
    }

    private void Update()
    {
        RefreshTarget();
        StateHandler();
        if (_enemyDamage.IsAlive) UpdateFacingDirection();
    }


    public bool IsRunning() => _navMeshAgent.velocity != Vector3.zero;

    public float GetRoamingAnimationSpeed() => _navMeshAgent.speed / _roamSpeed;


    private void RefreshTarget()
    {
        if (PartyManager.Instance == null) return;
        _currentTarget = PartyManager.Instance.GetNearestTarget(transform.position);
    }

    private bool HasLiveTarget() => _currentTarget != null && _currentTarget.IsAlive;

    private Vector3 TargetPosition() => _currentTarget.Transform.position;

    private float DistanceToTarget() =>
        HasLiveTarget()
            ? Vector3.Distance(transform.position, TargetPosition())
            : float.MaxValue;

    // ======= State machine ========================================================

    private void StateHandler()
    {
        switch (_state)
        {
            default:
            case State.Idle:
                if (!_isIdle)
                {
                    StartCoroutine(IdleState());
                }
                CheckCurrentState();
                break;

            case State.Roaming:
                if (!_isRoaming)
                {
                    _navMeshAgent.speed = _roamSpeed;
                    Roaming();
                    _roamingTime = _roamingTimerMax;
                }
                CheckCurrentState();
                _roamingTime -= Time.deltaTime;
                if (_roamingTime <= 0)
                {
                    _isRoaming = false;
                    SetShortPath(0.5f);
                    _state = State.Idle;
                }
                if (_navMeshAgent.destination == transform.position)
                    _roamingTime = 0;
                
                break;

            case State.Chasing:
                if (_useSteppedChase) SteppedChase();
                else ChasingTarget();
                CheckCurrentState();
                break;

            case State.Attacking:
                AttackingTarget();
                CheckCurrentState();
                break;

            case State.Retreating:
                RetreatFromTarget();
                CheckCurrentState();
                break;

            case State.Death:
                break;
        }
    }

    

    private void CheckCurrentState()
    {
        if (!_enemyDamage.IsAlive)
        {
            TransitionTo(State.Death);
            return;
        }
        bool canAttack = Time.time > _nextAttackTime;
        State newState = State.Roaming;

        if (_state == State.Idle)
        {
            newState = State.Idle;
        }

        if (HasLiveTarget())
        {
            float dist = DistanceToTarget();
            if (_isChasingEnemy)
            {
                if (_enemyDamage.InCombat) newState = State.Chasing;
                else if (dist <= _chasingDistance)
                {
                    newState = State.Chasing;
                    _enemyDamage.SetCombat(true);
                }
            }
            if (_isAttackingEnemy && dist <= _attackDistance) 
                newState = State.Attacking;
            if (_retreatAfterAttack && !canAttack && dist < _minSafeDistance && !IsAttacking)
                newState = State.Retreating;
        }

        if (newState != _state)
            TransitionTo(newState);
    }

    private void TransitionTo(State newState)
    {
        switch (newState)
        {
            case State.Chasing:
                _navMeshAgent.ResetPath();
                _navMeshAgent.speed = _chasingSpeed;
                break;
            case State.Roaming:
                _roamingTime = 0;
                _navMeshAgent.ResetPath();
                _navMeshAgent.speed = _roamSpeed;
                break;
            case State.Attacking:
                _navMeshAgent.ResetPath();
                break;
            case State.Retreating:
                _navMeshAgent.ResetPath();
                _navMeshAgent.speed = _retreatSpeed;
                _isRetreating = false;
                break;
            case State.Death:
                _navMeshAgent.ResetPath();
                break;
        }
        _state = newState;
    }

    // ========== Combat ====================================================

    private void AttackingTarget()
    {
        if (Time.time > _nextAttackTime && !_enemyDamage.InStagger) // && _navMeshAgent.velocity == Vector3.zero
        {
            OnEnemyAttack?.Invoke(this, EventArgs.Empty);
            IsAttacking = true;
            _nextAttackTime = Time.time + _attackRate;
            _navMeshAgent.ResetPath();
        }
    }

    private void ChasingTarget()
    {
        if (!IsAttacking && HasLiveTarget() && Time.time > _nextAttackTime)
            _navMeshAgent.SetDestination(TargetPosition());

    }

    private void SteppedChase()
    {
        if (IsAttacking || _isStepPausing) return;

        // pause
        if (_stepInProgress && !_navMeshAgent.pathPending
            && _navMeshAgent.remainingDistance < 0.15f)
        {
            _stepInProgress = false;
            StartCoroutine(StepPauseRoutine());
            return;
        }

        // next step
        if (!_stepInProgress && HasLiveTarget())
        {
            Vector3 toTarget = (TargetPosition() - transform.position).normalized;
            float spreadRad = UnityEngine.Random.Range(-_stepSpread, _stepSpread) * Mathf.Deg2Rad;
            Vector3 stepDir = new Vector3(
                toTarget.x * Mathf.Cos(spreadRad) - toTarget.y * Mathf.Sin(spreadRad),
                toTarget.x * Mathf.Sin(spreadRad) + toTarget.y * Mathf.Cos(spreadRad),
                0f).normalized;

            Vector3 stepTarget = transform.position + stepDir * _stepDistance;
            _navMeshAgent.SetDestination(stepTarget);
            _stepInProgress = true;
        }
    }

    private IEnumerator StepPauseRoutine()
    {
        _isStepPausing = true;
        _navMeshAgent.ResetPath();
        yield return new WaitForSeconds(_stepPauseDuration);
        _isStepPausing = false;
    }

    private void RetreatFromTarget()
    {
        if (!HasLiveTarget()) return;

        float dist = DistanceToTarget();
        if (dist >= _minSafeDistance)
        {
            _navMeshAgent.ResetPath();
            _isRetreating = false;
            return;
        }

        if (!_isRetreating)
        {
            Vector3 awayDir = (transform.position - TargetPosition()).normalized;
            float spreadRad = UnityEngine.Random.Range(-30f, 30f) * Mathf.Deg2Rad;
            Vector3 retreatDir = new Vector3(
                awayDir.x * Mathf.Cos(spreadRad) - awayDir.y * Mathf.Sin(spreadRad),
                awayDir.x * Mathf.Sin(spreadRad) + awayDir.y * Mathf.Cos(spreadRad),
                0f).normalized;

            Vector3 retreatTarget = transform.position + retreatDir * _retreatDistance;
            _navMeshAgent.SetDestination(retreatTarget);
            _isRetreating = true;
        }

        if (_isRetreating && !_navMeshAgent.pathPending
            && _navMeshAgent.remainingDistance < 0.2f)
            _isRetreating = false;

        
    }

    // ========== Roam ================================================

    private void Roaming()
    {
        if (!IsAttacking)
        {
            _isRoaming = true;
            _roamPosition = GetRoamPosition();
            _navMeshAgent.SetDestination(_roamPosition);
        }
    }

    private Vector3 GetRoamPosition()
    {
        if (_canChangeStartPos)
            _startingPosition = transform.position;
        return _startingPosition + Utils.GetRandomDir() * UnityEngine.Random.Range(_roamingDistanceMin, _roamingDistanceMax);
    }

    // shorts path on timeout (for smooth stop)
    private void SetShortPath(float finalStopDistance)
    {
        Vector3 toDestination = _navMeshAgent.destination - transform.position;
        if (toDestination.magnitude > finalStopDistance)
        {
            _navMeshAgent.SetDestination(transform.position + toDestination.normalized * finalStopDistance);
        }
    }

    IEnumerator IdleState()
    {
        _isIdle = true;
        yield return new WaitForSeconds(_idleDuration + UnityEngine.Random.Range(-1f, 1f));
        _state = State.Roaming;
        _isIdle = false;
    }


    // ======= Visual ====================================================
    private void UpdateFacingDirection()
    {
        Vector3 moveDir = _navMeshAgent.velocity.normalized;
        if (moveDir.magnitude > 0.1f)
        {
            bool shouldFaceRight = moveDir.x > 0;
            if (shouldFaceRight != IsFacingRight)
            {
                IsFacingRight = shouldFaceRight;
                OnEnemyUpdateSpriteDirection?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    //private void LookAtTarget()
    //{
    //    Vector2 difference = transform.position - TargetPosition();
    //    if (IsFacingRight && difference.x > 0f)
    //    {
    //        IsFacingRight = false;
    //        OnEnemyUpdateSpriteDirection?.Invoke(this, EventArgs.Empty);
    //    }
    //    else if (!IsFacingRight && difference.x < 0f)
    //    {
    //        IsFacingRight = true;
    //        OnEnemyUpdateSpriteDirection?.Invoke(this, EventArgs.Empty);
    //    }
    //}

    // ========== Initializing =========================================
    private void InitializeStats()
    {
        _enableRoam = _enemySO.EnableRoam;
        _canChangeStartPos = _enemySO.EnableChangeStartPos;
        _roamSpeed = _enemySO.RoamSpeed;
        _roamingDistanceMax = _enemySO.RoamingDistanceMax;
        _roamingDistanceMin = _enemySO.RoamingDistanceMin;
        _roamingTimerMax = _enemySO.RoamingTimerMax;
        _idleDuration = _enemySO.IdleDuration;

        _useSteppedChase = _enemySO.UseSteppedChase;
        _stepDistance = _enemySO.StepDistance;
        _stepSpread = _enemySO.StepSpread;
        _stepPauseDuration = _enemySO.StepPauseDuration;

        _isChasingEnemy = _enemySO.EnableChase;
        _chasingSpeed = _enemySO.ChasingSpeed;
        _chasingDistance = _enemySO.ChasingDistance;

        _isAttackingEnemy = _enemySO.EnableAttack;
        _attackDistance = _enemySO.AttackDistance;
        _attackRate = _enemySO.AttackRate;
    }
}
