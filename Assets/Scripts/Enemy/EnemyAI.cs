using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using GameUtils;
using System;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private State _startingState;
    [Header("Roaming settings")]
    [SerializeField] private float _roamSpeed = 1.5f;
    [SerializeField] private float _roamingDistanceMax = 7f;
    [SerializeField] private float _roamingDistanceMin = 1f;
    [SerializeField] private float _roamingTimerMax = 2f;
    [SerializeField] private float _idleDuration = 5f;
    [SerializeField] private bool _canChangeStartPos = true;
    private float _roamingTime;
    private Vector3 _roamPosition;
    private Vector3 _startingPosition; // walk around start coordinate

    [Header("Chase settings")]
    [SerializeField] private float _chasingSpeed = 2.5f;
    [SerializeField] private float _chasingDistance = 5f;
    [SerializeField] private float _chasingAnimationSpeedMultiplier = 1.5f;
    [SerializeField] private bool _isChasingEnemy = true;

    [Header("Attack settings")]
    [SerializeField] private bool _isAttackingEnemy = true;
    [SerializeField] private float _attackDistance = 1f;
    [SerializeField] private float _attackRate = 3f;
    private float _nextAttackTime = 0f;
    
    
    private Vector2 _originScale;

    private NavMeshAgent _navMeshAgent;
    [SerializeField] private State _state;

    private bool _isIdle = false;
    //private bool isChasing = false;
    private bool _isRoaming = false;
    private bool _isFacingRight = true;

    public event EventHandler OnEnemyAttack;
    public bool IsAttacking = false;


    private enum State
    {
        Idle,
        Roaming,
        Chasing,
        Attacking,
        Death
    }

    private void Awake()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _navMeshAgent.updateRotation = false;
        _navMeshAgent.updateUpAxis = false;
        _state = _startingState;
    }

    private void Start()
    {
        _startingPosition = transform.position;
        _navMeshAgent.speed = _roamSpeed;
        _originScale = transform.localScale;
    }

    private void Update()
    {
        StateHandler();
        UpdateFacingDirection();
    }


    public bool IsRunning()
    {
        if (_navMeshAgent.velocity == Vector3.zero)
            return false;
        else
            return true;
    }

    public float GetRoamingAnimationSpeed()
    {
        return _navMeshAgent.speed / _roamSpeed;
    }

    public void SetDeathState()
    {
        _navMeshAgent.ResetPath();
        _state = State.Death;
    }

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
                ChasingTarget();
                CheckCurrentState();
                break;
            case State.Attacking:
                AttackingTarget();
                CheckCurrentState();
                break;
            case State.Death:
                break;
        }
    }

    

    private void CheckCurrentState()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, PlayerMovement.Instance.transform.position);
        State newState = State.Roaming;
        if (_state == State.Idle)
        {
            newState = State.Idle;
        }

        if (_isChasingEnemy)
        {
            if (distanceToPlayer <= _chasingDistance)
            {
                newState = State.Chasing;
            }
        }

        if (_isAttackingEnemy)
        {
            if (distanceToPlayer <= _attackDistance)
            {
                newState = State.Attacking;
            }
        }

        if (newState != _state)
        {
            if (newState == State.Chasing)
            {
                _navMeshAgent.ResetPath();
                _navMeshAgent.speed = _chasingSpeed;
            }
            else if (newState == State.Roaming)
            {
                _roamingTime = 0;
                _navMeshAgent.ResetPath();
                _navMeshAgent.speed = _roamSpeed;
            }
            else if (newState == State.Attacking)
            {
                _navMeshAgent.ResetPath();
            }

                _state = newState;
        }
    }

    private void AttackingTarget()
    {
        if (Time.time > _nextAttackTime && _navMeshAgent.velocity == Vector3.zero)
        {
            OnEnemyAttack?.Invoke(this, EventArgs.Empty);
            
            _nextAttackTime = Time.time + _attackRate;
        }
    }

    private void ChasingTarget()
    {
        if (!IsAttacking)
            _navMeshAgent.SetDestination(PlayerMovement.Instance.transform.position);

    }

    private void Roaming()
    {
        _isRoaming = true;
        _roamPosition = GetRoamPosition();
        _navMeshAgent.SetDestination(_roamPosition);
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
            Vector3 shortenedDestination = transform.position +
                toDestination.normalized * finalStopDistance;
            _navMeshAgent.SetDestination(shortenedDestination);
        }
    }

    IEnumerator IdleState()
    {
        _isIdle = true;
        yield return new WaitForSeconds(_idleDuration + UnityEngine.Random.Range(-1f, 1f));
        _state = State.Roaming;
        _isIdle = false;
    }

    private void UpdateFacingDirection()
    {
        // ѕолучаем текущее направление движени€
        Vector3 moveDirection = _navMeshAgent.velocity.normalized;

        if (moveDirection.magnitude > 0.1f)
        {
            // ќпредел€ем, вправо или влево движемс€
            bool shouldFaceRight = moveDirection.x > 0;

            // –азворачиваем только если направление изменилось
            if (shouldFaceRight != _isFacingRight)
            {
                FlipDirection(shouldFaceRight);
                _isFacingRight = shouldFaceRight;
            }
        }
    }

    private void FlipDirection(bool faceRight)
    {
        if (faceRight)
            transform.localScale = new Vector3(_originScale.x, _originScale.y, 1);
        else if (!faceRight)
            transform.localScale = new Vector3(-_originScale.x, _originScale.y, 1);
    }

}
