using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>
/// Behavior node that drives ML combat movement (positioning, escape, follow leader).
/// NavMesh steering is handled by <see cref="AllyMLMovementModifier"/>.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Ally ML Combat Movement",
    story: "[Agent] uses ML combat movement on [Target]",
    category: "Action/Navigation",
    id: "a1b2c3d4e5f60718293a4b5c6d7e8f90")]
public partial class AllyMLCombatMovementAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<GameObject> Leader;
    [SerializeReference] public BlackboardVariable<float> HealthPercent;
    [SerializeReference] public BlackboardVariable<float> LowHealthThreshold;
    [SerializeReference] public BlackboardVariable<float> MaxDistToLeader;
    [SerializeReference] public BlackboardVariable<float> LowHealthHysteresis;

    protected override Status OnStart()
    {
        if (Agent.Value == null)
            return Status.Failure;

        if (Agent.Value.TryGetComponent(out AllyMLMovementModifier modifier))
            modifier.SetGraphDriving(true);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent.Value == null)
            return Status.Failure;

        if (!Agent.Value.TryGetComponent(out AllyMLMovementModifier modifier))
            return Status.Failure;

        if (!Agent.Value.TryGetComponent(out AllyAIBrain brain))
            return Status.Failure;

        SyncMovementContext(brain, modifier);
        SyncTarget(brain);
        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (Agent.Value != null && Agent.Value.TryGetComponent(out AllyMLMovementModifier modifier))
            modifier.SetGraphDriving(false);
    }

    private void SyncMovementContext(AllyAIBrain brain, AllyMLMovementModifier modifier)
    {
        float healthPercent = HealthPercent != null
            ? HealthPercent.Value
            : brain.HealthPercent * 100f;

        float lowHealthThreshold = LowHealthThreshold != null
            ? LowHealthThreshold.Value
            : GetProfileLowHealthThreshold(brain);

        float maxDistToLeader = MaxDistToLeader != null
            ? MaxDistToLeader.Value
            : 10f;

        float hysteresis = LowHealthHysteresis != null ? LowHealthHysteresis.Value : -1f;
        modifier.SetMovementContext(healthPercent, lowHealthThreshold, maxDistToLeader, hysteresis);
    }

    private void SyncTarget(AllyAIBrain brain)
    {
        if (Target.Value != null)
            return;

        Transform weaponTarget = brain.CurrentWeaponTarget;
        if (weaponTarget != null)
            Target.Value = weaponTarget.gameObject;
    }

    private static float GetProfileLowHealthThreshold(AllyAIBrain brain)
    {
        if (brain.Profile != null)
            return brain.Profile.RetreatHealthThreshold * 100f;

        return 20f;
    }
}
