using UnityEngine;
using UnityEngine.AI;

public class AgentConfiner : MonoBehaviour
{
    private void Awake()
    {
        if (TryGetComponent(out NavMeshAgent agent))
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
    }
}
