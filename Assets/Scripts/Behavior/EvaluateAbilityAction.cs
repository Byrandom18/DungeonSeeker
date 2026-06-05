using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Evaluate Ability", story: "[Agent] evaluates best ability for [Target]", category: "Action", id: "b2c3d4e5f6789012345678abcdef0123")]
public partial class EvaluateAbilityAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<int> AbilitySlot;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> AbilityScore;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent.Value == null || !Agent.Value.TryGetComponent(out AllyAIBrain brain))
            return Status.Failure;

        AbilityDecision decision = brain.SelectAbility(forceRefresh: true);

        if (AbilityScore != null)
            AbilityScore.Value = decision.Score;

        if (!decision.IsValid)
            return Status.Failure;

        if (AbilitySlot != null)
            AbilitySlot.Value = decision.SlotIndex;

        if (Target != null && decision.Target != null)
            Target.Value = decision.Target.gameObject;

        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}
