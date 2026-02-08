using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using GameUtils;
using System;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private State startingState;
    [Header("Roaming settings")]
    [SerializeField] private float roamSpeed = 1.5f;
    [SerializeField] private float roamingDistanceMax = 7f;
    [SerializeField] private float roamingDistanceMin = 1f;
    [SerializeField] private float roamingTimerMax = 2f;
    [SerializeField] private float idleDuration = 5f;
    [SerializeField] private bool canChangeStartPos = true;
    private float roamingTime;
    private Vector3 roamPosition;
    private Vector3 startingPosition; // walk around start coordinate

    [Header("Chase settings")]
    [SerializeField] private float chasingSpeed = 2.5f;
    [SerializeField] private float chasingDistance = 5f;
    [SerializeField] private float chasingAnimationSpeedMultiplier = 1.5f;
    [SerializeField] private bool isChasingEnemy = true;

    [Header("Attack settings")]
    [SerializeField] private bool isAttackingEnemy = true;
    [SerializeField] private float attackDistance = 1f;
    [SerializeField] private float attackRate = 3f;
    private float nextAttackTime = 0f;
    
    
    private Vector2 originScale;

    private NavMeshAgent navMeshAgent;
    [SerializeField] private State state;

    private bool isIdle = false;
    //private bool isChasing = false;
    private bool isRoaming = false;
    private bool isFacingRight = true;

    public event EventHandler OnEnemyAttack;



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
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.updateRotation = false;
        navMeshAgent.updateUpAxis = false;
        state = startingState;
    }

    private void Start()
    {
        startingPosition = transform.position;
        navMeshAgent.speed = roamSpeed;
        originScale = transform.localScale;
    }

    private void Update()
    {
        StateHandler();
        UpdateFacingDirection();
    }


    public bool IsRunning()
    {
        if (navMeshAgent.velocity == Vector3.zero)
            return false;
        else
            return true;
    }

    public float GetRoamingAnimationSpeed()
    {
        return navMeshAgent.speed / roamSpeed;
    }

    private void StateHandler()
    {
        switch (state)
        {
            default:
            case State.Idle:
                if (!isIdle)
                {
                    StartCoroutine(IdleState());
                }
                CheckCurrentState();
                break;

            case State.Roaming:
                if (!isRoaming)
                {
                    navMeshAgent.speed = roamSpeed;
                    Roaming();
                    roamingTime = roamingTimerMax;
                }
                CheckCurrentState();
                roamingTime -= Time.deltaTime;
                if (roamingTime <= 0)
                {
                    isRoaming = false;
                    SetShortPath(0.5f);
                    state = State.Idle;
                }
                if (navMeshAgent.destination == transform.position)
                    roamingTime = 0;
                
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
        if (state == State.Idle)
        {
            newState = State.Idle;
        }

        if (isChasingEnemy)
        {
            if (distanceToPlayer <= chasingDistance)
            {
                newState = State.Chasing;
            }
        }

        if (isAttackingEnemy)
        {
            if (distanceToPlayer <= attackDistance)
            {
                newState = State.Attacking;
            }
        }

        if (newState != state)
        {
            if (newState == State.Chasing)
            {
                navMeshAgent.ResetPath();
                navMeshAgent.speed = chasingSpeed;
            }
            else if (newState == State.Roaming)
            {
                roamingTime = 0;
                navMeshAgent.ResetPath();
                navMeshAgent.speed = roamSpeed;
            }
            else if (newState == State.Attacking)
            {
                navMeshAgent.ResetPath();
            }

                state = newState;
        }
    }

    private void AttackingTarget()
    {
        if (Time.time > nextAttackTime)
        {
            OnEnemyAttack?.Invoke(this, EventArgs.Empty);

            nextAttackTime = Time.time + attackRate;
        }
    }

    private void ChasingTarget()
    {
        navMeshAgent.SetDestination(PlayerMovement.Instance.transform.position);

    }

    private void Roaming()
    {
        isRoaming = true;
        roamPosition = GetRoamPosition();
        navMeshAgent.SetDestination(roamPosition);
    }

    private Vector3 GetRoamPosition()
    {
        if (canChangeStartPos)
            startingPosition = transform.position;
        return startingPosition + Utils.GetRandomDir() * UnityEngine.Random.Range(roamingDistanceMin, roamingDistanceMax);
    }

    // shorts path on timeout (for smooth stop)
    private void SetShortPath(float finalStopDistance)
    {
        Vector3 toDestination = navMeshAgent.destination - transform.position;
        if (toDestination.magnitude > finalStopDistance)
        {
            Vector3 shortenedDestination = transform.position +
                toDestination.normalized * finalStopDistance;
            navMeshAgent.SetDestination(shortenedDestination);
        }
    }

    IEnumerator IdleState()
    {
        isIdle = true;
        yield return new WaitForSeconds(idleDuration + UnityEngine.Random.Range(-1f, 1f));
        state = State.Roaming;
        isIdle = false;
    }

    private void UpdateFacingDirection()
    {
        // ѕолучаем текущее направление движени€
        Vector3 moveDirection = navMeshAgent.velocity.normalized;

        if (moveDirection.magnitude > 0.1f)
        {
            // ќпредел€ем, вправо или влево движемс€
            bool shouldFaceRight = moveDirection.x > 0;

            // –азворачиваем только если направление изменилось
            if (shouldFaceRight != isFacingRight)
            {
                FlipDirection(shouldFaceRight);
                isFacingRight = shouldFaceRight;
            }
        }
    }

    private void FlipDirection(bool faceRight)
    {
        if (faceRight)
            transform.localScale = new Vector3(originScale.x, originScale.y, 1);
        else if (!faceRight)
            transform.localScale = new Vector3(-originScale.x, originScale.y, 1);
    }

}
