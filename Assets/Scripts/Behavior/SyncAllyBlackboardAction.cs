using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Sync Ally Blackboard", story: "Sync [Agent] stats to blackboard", category: "Action", id: "a1b2c3d4e5f6789012345678abcdef01")]
public partial class SyncAllyBlackboardAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<float> HealthPercent;
    [SerializeReference] public BlackboardVariable<float> ManaPercent;
    [SerializeReference] public BlackboardVariable<float> AttackDistance;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent.Value == null)
            return Status.Failure;

        if (Agent.Value.TryGetComponent(out AllyAIBrain brain))
        {
            brain.RefreshSnapshot();

            if (HealthPercent != null)
                HealthPercent.Value = brain.HealthPercent * 100f;

            if (ManaPercent != null)
                ManaPercent.Value = brain.ManaPercent * 100f;

            if (AttackDistance != null)
                AttackDistance.Value = brain.AttackDistance;

            return Status.Success;
        }

        if (Agent.Value.TryGetComponent(out PlayerStats stats))
        {
            if (HealthPercent != null && stats.MaxHealth > 0f)
                HealthPercent.Value = stats.Health / stats.MaxHealth * 100f;

            if (ManaPercent != null && stats.MaxMana > 0f)
                ManaPercent.Value = stats.Mana / stats.MaxMana * 100f;
        }

        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}
