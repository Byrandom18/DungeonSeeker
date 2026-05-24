using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Retreat", story: "[Agent] [Retreats] from [Target] by [SafeDistance]", category: "Action", id: "4fff9c6f18ab68d49082beb5a1c82312")]
public partial class RetreatAction : Action
{
    public enum RetreatMode
    {
        Retreat,
        Escape
    }

    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<RetreatMode> Retreats;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> SafeDistance;
    [SerializeReference] public BlackboardVariable<float> Speed = new BlackboardVariable<float>(3f);

    private const float k_SampleRadius = 2f; // How far are we looking for a point of departure from the agent's current position using NavMesh
    private const float k_ProjectDistance = 1.5f; // Step forward along the retreat vector when searching for a point on the NavMesh
    private NavMeshAgent m_NavMeshAgent;
    [CreateProperty] private float m_OriginalSpeed = -1f;
    [CreateProperty] private float m_OriginalStoppingDistance = -1f;


    protected override Status OnStart()
    {
        if (Agent.Value == null || Target.Value == null)
            return Status.Failure;

        m_NavMeshAgent = Agent.Value.GetComponent<NavMeshAgent>();

        if (m_NavMeshAgent != null)
        {
            m_OriginalSpeed = m_NavMeshAgent.speed;
            m_OriginalStoppingDistance = m_NavMeshAgent.stoppingDistance;

            m_NavMeshAgent.speed = Speed.Value;
            m_NavMeshAgent.stoppingDistance = 0f;

            if (m_NavMeshAgent.isOnNavMesh)
                m_NavMeshAgent.ResetPath();
        }

        return UpdateDestination() ? Status.Running : Status.Failure;
    }

    protected override Status OnUpdate()
    {
        if (Agent.Value == null || Target.Value == null)
            return Status.Failure;

        float dist2D = Vector2.Distance(Agent.Value.transform.position, Target.Value.transform.position);
        if (dist2D >= SafeDistance.Value)
            return Status.Success;

        if (!UpdateDestination())
            return Status.Failure;


        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (m_NavMeshAgent != null)
        {
            if (m_NavMeshAgent.isOnNavMesh)
                m_NavMeshAgent.ResetPath();

            if (m_OriginalSpeed >= 0f)
                m_NavMeshAgent.speed = m_OriginalSpeed;
            if (m_OriginalStoppingDistance >= 0f)
                m_NavMeshAgent.stoppingDistance = m_OriginalStoppingDistance;
        }

        m_NavMeshAgent = null;
    }

    private bool UpdateDestination()
    {
        Vector3 agentPos = Agent.Value.transform.position;
        Vector3 targetPos = Target.Value.transform.position;

        Vector3 retreatDir = GetRetreatDirection(agentPos, targetPos);
        if (retreatDir == Vector3.zero)
            return false;

        Vector3 destination = agentPos + retreatDir * k_ProjectDistance;

        if (m_NavMeshAgent != null)
        {
            if (!NavMesh.SamplePosition(destination, out NavMeshHit hit, k_SampleRadius, NavMesh.AllAreas))
                return false;

            if (Retreats.Value == RetreatMode.Retreat)
            {
                float dist2D = Vector2.Distance(Agent.Value.transform.position, Target.Value.transform.position);
                float safedist = SafeDistance.Value;
                float t = Mathf.Clamp01((safedist - dist2D) / Mathf.Max(0.01f, safedist));
                m_NavMeshAgent.speed = Mathf.Max(Speed.Value * 0.1f, Speed.Value * t);
            }

            m_NavMeshAgent.isStopped = false;
            m_NavMeshAgent.SetDestination(hit.position);
        }
        else
        {
            Agent.Value.transform.position = Vector3.MoveTowards(
                agentPos, destination, Speed.Value * Time.deltaTime);
        }

        return true;
    }

    private Vector3 GetRetreatDirection(Vector3 agentPos, Vector3 targetPos)
    {
        Vector3 awayFromTarget = agentPos - targetPos;
        awayFromTarget.z = 0f;

        if (awayFromTarget == Vector3.zero)
            awayFromTarget = Vector3.right; // fallback

        awayFromTarget.Normalize();

        if (Retreats.Value == RetreatMode.Retreat)
            return awayFromTarget;

        // Escape
        Vector3 toAlly = GetDirectionToNearestAlly(agentPos);

        if (toAlly == Vector3.zero)
            return awayFromTarget; // no ally — Retreat

        Vector3 combined = (awayFromTarget + toAlly).normalized;
        return combined == Vector3.zero ? awayFromTarget : combined;
    }

    private Vector3 GetDirectionToNearestAlly(Vector3 agentPos)
    {
        if (PartyManager.Instance == null) return Vector3.zero;

        ICharacterEntity nearest = null;
        float minDistSq = float.MaxValue;

        foreach (ICharacterEntity member in PartyManager.Instance.Members)
        {
            if (!member.IsAlive) continue;

            if (member.Transform == Agent.Value.transform) continue;

            float sq = ((Vector2)member.Transform.position - (Vector2)agentPos).sqrMagnitude;
            if (sq < minDistSq)
            {
                minDistSq = sq;
                nearest = member;
            }
        }

        if (nearest == null || minDistSq < 1.5f * 1.5f) return Vector3.zero;

        Vector3 dir = nearest.Transform.position - agentPos;
        dir.z = 0f;
        return dir.normalized;
    }
}

