using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

public class AgentConfiner : MonoBehaviour
{
    //[SerializeField] private BehaviorGraphAgent _agent;

    private void Awake()
    {
        if (TryGetComponent(out NavMeshAgent agent))
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
    }
}
