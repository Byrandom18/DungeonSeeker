using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>
/// Behavior node that keeps ML combat movement active while in combat.
/// Actual NavMesh steering is handled by <see cref="AllyMLMovementModifier"/>.
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

    protected override Status OnStart()
    {
        if (Agent.Value == null)
            return Status.Failure;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent.Value == null)
            return Status.Failure;

        if (!Agent.Value.TryGetComponent(out AllyMLMovementModifier modifier))
            return Status.Failure;

        if (!modifier.ShouldControlMovement())
            return Status.Success;

        if (Target.Value == null && Agent.Value.TryGetComponent(out AllyAIBrain brain))
        {
            Transform weaponTarget = brain.CurrentWeaponTarget;
            if (weaponTarget != null)
                Target.Value = weaponTarget.gameObject;
        }

        return Status.Running;
    }
}
