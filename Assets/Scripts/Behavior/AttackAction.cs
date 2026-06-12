using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Attack", story: "[Agent] attacks [Target]", category: "Action", id: "0d3a4992dc162a5bd4876a8c2b540ab9")]
public partial class AttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<ActiveWeapon> Weapon;
    [SerializeReference] public BlackboardVariable<float> AttackCooldown;

    private bool _attackInProgress;
    private bool _attackComplete;

    protected override Status OnStart()
    {
        _attackInProgress = false;
        _attackComplete = false;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Target.Value == null || Weapon.Value == null)
            return Status.Failure;

        WeaponBase weapon = Weapon.Value.GetActiveWeapon();
        if (weapon == null)
            return Status.Failure;

        if (_attackComplete)
            return Status.Success;

        if (_attackInProgress)
            return Status.Running;

        Weapon.Value.SetWorldAimTarget(Target.Value.transform);
        _attackInProgress = true;

        if (AttackCooldown != null)
            AttackCooldown.Value = weapon.Cooldown;

        Agent.Value.GetComponent<MonoBehaviour>()
            .StartCoroutine(ExecuteAttack(Weapon.Value, weapon));

        return Status.Running;
    }

    protected override void OnEnd()
    {
        _attackInProgress = false;
        _attackComplete = false;

        // CooldownModifier reads Duration on the next OnStart; reset so a stale value
        // does not pre-apply an extra wait before the child can run.
        if (AttackCooldown != null)
            AttackCooldown.Value = 0f;
    }

    private IEnumerator ExecuteAttack(ActiveWeapon activeWeapon, WeaponBase weapon)
    {
        activeWeapon.NotifyAttackStarted();
        weapon.Attack();

        yield return new WaitForSeconds(weapon.Cooldown);

        activeWeapon.NotifyAttackEnded();
        _attackInProgress = false;
        _attackComplete = true;
    }
}

