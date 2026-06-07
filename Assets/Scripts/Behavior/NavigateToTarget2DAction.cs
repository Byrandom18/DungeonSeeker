using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Navigate To Target 2D", story: "[Agent] navigates to [Target]", category: "Action/Navigation", id: "80e9f040f5372732148a111440962bdd")]
public partial class NavigateToTarget2DAction : Action
{
    public enum TargetPositionMode
    {
        ClosestPointOnAnyCollider,      // Use the closest point on any collider, including child objects
        ClosestPointOnTargetCollider,   // Use the closest point on the target's own collider only
        ExactTargetPosition             // Use the exact position of the target, ignoring colliders
    }

    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> Speed = new BlackboardVariable<float>(1.0f);
    [SerializeReference] public BlackboardVariable<float> DistanceThreshold = new BlackboardVariable<float>(0.2f);

    // This will only be used in movement without a navigation agent.
    [SerializeReference] public BlackboardVariable<float> SlowDownDistance = new BlackboardVariable<float>(1.0f);

    [Tooltip("Defines how the target position is determined for navigation:" +
        "\n- ClosestPointOnAnyCollider: Use the closest point on any collider, including child objects" +
        "\n- ClosestPointOnTargetCollider: Use the closest point on the target's own collider only" +
        "\n- ExactTargetPosition: Use the exact position of the target, ignoring colliders. Default if no collider is found.")]
    [SerializeReference] public BlackboardVariable<TargetPositionMode> m_TargetPositionMode = new(TargetPositionMode.ClosestPointOnAnyCollider);

    private NavMeshAgent m_NavMeshAgent;
    private Vector3 m_LastTargetPosition;
    private Vector3 m_ColliderAdjustedTargetPosition;
    [CreateProperty] private float m_OriginalStoppingDistance = -1f;
    [CreateProperty] private float m_OriginalSpeed = -1f;
    private float m_ColliderOffset;

    protected override Status OnStart()
    {
        if (Agent.Value == null || Target.Value == null)
        {
            return Status.Failure;
        }

        return Initialize();
    }

    protected override Status OnUpdate()
    {
        if (Agent.Value == null || Target.Value == null)
        {
            return Status.Failure;
        }

        if (Agent.Value.TryGetComponent(out AllyMLMovementModifier mlMovement) && mlMovement.IsControllingMovement)
            return Status.Running;

        // Check if the target position has changed.
        bool boolUpdateTargetPosition =
            !Mathf.Approximately(m_LastTargetPosition.x, Target.Value.transform.position.x)
            || !Mathf.Approximately(m_LastTargetPosition.y, Target.Value.transform.position.y);

        if (boolUpdateTargetPosition)
        {
            m_LastTargetPosition = Target.Value.transform.position;
            m_ColliderAdjustedTargetPosition = GetPositionColliderAdjusted();
        }

        float distance = GetDistance2D();
        bool destinationReached = distance <= (DistanceThreshold + m_ColliderOffset);

        if (destinationReached && (m_NavMeshAgent == null || !m_NavMeshAgent.pathPending))
        {
            return Status.Success;
        }
        else if (m_NavMeshAgent == null) // transform-based movement
        {
            MoveTowardsLocation2D(m_ColliderAdjustedTargetPosition, distance);
        }
        else if (boolUpdateTargetPosition) // navmesh-based destination update (if needed)
        {
            m_NavMeshAgent.SetDestination(m_ColliderAdjustedTargetPosition);
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (m_NavMeshAgent != null)
        {
            if (m_NavMeshAgent.isOnNavMesh)
            {
                m_NavMeshAgent.ResetPath();
            }
            m_NavMeshAgent.speed = m_OriginalSpeed;
            m_NavMeshAgent.stoppingDistance = m_OriginalStoppingDistance;
        }

        m_NavMeshAgent = null;
    }

    protected override void OnDeserialize()
    {
        m_NavMeshAgent = Agent.Value.GetComponentInChildren<NavMeshAgent>();
        if (m_NavMeshAgent != null)
        {
            if (m_OriginalSpeed >= 0f)
                m_NavMeshAgent.speed = m_OriginalSpeed;
            if (m_OriginalStoppingDistance >= 0f)
                m_NavMeshAgent.stoppingDistance = m_OriginalStoppingDistance;

            m_NavMeshAgent.Warp(Agent.Value.transform.position);
        }

        Initialize();
    }


    private Status Initialize()
    {
        m_LastTargetPosition = Target.Value.transform.position;
        m_ColliderAdjustedTargetPosition = GetPositionColliderAdjusted();

        // Add the extents of the colliders to the stopping distance.
        m_ColliderOffset = 0.0f;
        Collider2D agentCollider = Agent.Value.GetComponentInChildren<Collider2D>();
        if (agentCollider != null)
        {
            Vector2 colliderExtents = agentCollider.bounds.extents;
            m_ColliderOffset += Mathf.Max(colliderExtents.x, colliderExtents.y);
        }

        if (GetDistance2D() <= (DistanceThreshold + m_ColliderOffset))
        {
            return Status.Success;
        }

        m_NavMeshAgent = Agent.Value.GetComponentInChildren<NavMeshAgent>();
        if (m_NavMeshAgent != null)
        {
            if (m_NavMeshAgent.isOnNavMesh)
            {
                m_NavMeshAgent.ResetPath();
            }

            m_OriginalSpeed = m_NavMeshAgent.speed;
            m_NavMeshAgent.speed = Speed;
            m_OriginalStoppingDistance = m_NavMeshAgent.stoppingDistance;
            m_NavMeshAgent.stoppingDistance = DistanceThreshold + m_ColliderOffset;
            m_NavMeshAgent.SetDestination(m_ColliderAdjustedTargetPosition);
        }

        return Status.Running;
    }

    private Vector3 GetPositionColliderAdjusted()
    {
        switch (m_TargetPositionMode.Value)
        {
            case TargetPositionMode.ClosestPointOnAnyCollider:
                Collider2D anyCollider = Target.Value.GetComponentInChildren<Collider2D>(includeInactive: false);
                if (anyCollider == null || anyCollider.enabled == false)
                    break;
                return anyCollider.ClosestPoint(Agent.Value.transform.position);
            case TargetPositionMode.ClosestPointOnTargetCollider:
                Collider2D targetCollider = Target.Value.GetComponent<Collider2D>();
                if (targetCollider == null || targetCollider.enabled == false)
                    break;
                return targetCollider.ClosestPoint(Agent.Value.transform.position);
        }

        // Default to target position.
        return Target.Value.transform.position;
    }

    /// <summary>
    /// 2D distance ignores the Z axis entirely.
    /// </summary>
    private float GetDistance2D()
    {
        Vector2 agentPos = Agent.Value.transform.position;
        Vector2 targetPos = m_ColliderAdjustedTargetPosition;
        return Vector2.Distance(agentPos, targetPos);
    }

    /// <summary>
    /// Simple transform-based movement in 2D with optional slow-down near the target.
    /// No rotation is applied.
    /// </summary>
    private void MoveTowardsLocation2D(Vector3 targetPosition, float distance)
    {
        float slowDownDist = SlowDownDistance.Value;
        float speedFactor = (slowDownDist > 0f && distance < slowDownDist)
            ? Mathf.Clamp01(distance / slowDownDist)
            : 1f;

        Vector2 direction = ((Vector2)targetPosition - (Vector2)Agent.Value.transform.position).normalized;
        Vector2 delta = direction * (Speed.Value * speedFactor * Time.deltaTime);

        Agent.Value.transform.position += new Vector3(delta.x, delta.y, 0f);
    }
}


