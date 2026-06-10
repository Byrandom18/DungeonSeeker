using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Ability Perform", story: "[Agent] perform ability in [Target]", category: "Action", id: "45d69a4463ea3faa2a225e152485c7ce")]
public partial class AbilityPerformAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<int> AbilitySlot;
    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent.Value == null)
            return Status.Failure;

        if (!Agent.Value.TryGetComponent(out AllyAIBrain brain))
            return UseFixedSlotFallback();

        AbilityDecision decision = brain.SelectAbility(forceRefresh: true);
        if (!decision.IsValid)
            return Status.Running;

        if (!Agent.Value.TryGetComponent(out AbilitySystem abilitySystem))
            return Status.Failure;

        if (!abilitySystem.UseAbility(decision.SlotIndex, decision.AimPosition))
            return Status.Running;

        if (AbilitySlot != null)
            AbilitySlot.Value = decision.SlotIndex;

        if (Target != null && decision.Target != null)
            Target.Value = decision.Target.gameObject;

        return Status.Running;
    }

    protected override void OnEnd()
    {
    }

    private Status UseFixedSlotFallback()
    {
        if (Agent.Value == null || AbilitySlot == null)
            return Status.Failure;

        if (!Agent.Value.TryGetComponent(out AbilitySystem abilitySystem))
            return Status.Failure;

        Vector3 aim = Agent.Value.transform.position;
        if (Target.Value != null)
            aim = Target.Value.transform.position;

        abilitySystem.UseAbility(AbilitySlot.Value, aim);
        return Status.Running;
    }
}
