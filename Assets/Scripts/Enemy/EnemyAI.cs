using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using GameUtils;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private State startingState;
    [SerializeField] private float roamSpeed = 1.5f;
    [SerializeField] private float roamingDistanceMax = 7f;
    [SerializeField] private float roamingDistanceMin = 1f;
    [SerializeField] private float roamingTimerMax = 2f;
    [SerializeField] private float idleDuration = 5f;

    [SerializeField] private float roamingTime;
    [SerializeField] private bool canChangeStartPos = true;
    private Vector3 roamPosition;
    private Vector3 startingPosition; // walk around start coordinate
    private Vector2 originScale;

    private NavMeshAgent navMeshAgent;
    [SerializeField] private State state;

    private bool isIdle = false;
    private bool isChasing = false;
    private bool isRoaming = false;
    private bool isFacingRight = true;

    private enum State
    {
        Idle,
        Roaming
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
        switch (state)
        {
            default:
            case State.Idle:
                if (!isIdle && !isChasing)
                {
                    StartCoroutine(IdleState());
                }
                break;

            case State.Roaming:
                if (!isRoaming && !isChasing)
                {
                    navMeshAgent.speed = roamSpeed;
                    Roaming();
                    roamingTime = roamingTimerMax;
                }

                roamingTime -= Time.deltaTime;
                if (roamingTime <= 0)
                {
                    isRoaming = false;
                    SetShortPath(0.5f);
                    state = State.Idle;
                }
                break;
        }
        UpdateFacingDirection();
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
        return startingPosition + Utils.GetRandomDir() * Random.Range(roamingDistanceMin, roamingDistanceMax);
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
        yield return new WaitForSeconds(idleDuration + Random.Range(-1f, 1f));
        if (!isChasing)
        {
            state = State.Roaming; 
        }

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
