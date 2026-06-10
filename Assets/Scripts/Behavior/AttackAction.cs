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

    protected override Status OnStart()
    {
        _attackInProgress = false;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Target.Value == null || Weapon.Value == null)
            return Status.Failure;
        WeaponBase weapon = Weapon.Value.GetActiveWeapon();
        if (weapon == null)
            return Status.Failure;

        Weapon.Value.SetWorldAimTarget(Target.Value.transform);

        if (_attackInProgress)
            return Status.Running;

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
    }

    private IEnumerator ExecuteAttack(ActiveWeapon activeWeapon, WeaponBase weapon)
    {
        activeWeapon.NotifyAttackStarted();
        weapon.Attack();

        yield return new WaitForSeconds(weapon.Cooldown);

        activeWeapon.NotifyAttackEnded();
        _attackInProgress = false;
    }
}

