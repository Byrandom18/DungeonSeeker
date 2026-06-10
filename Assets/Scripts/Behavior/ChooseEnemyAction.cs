using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ChooseEnemy", story: "Find best [Enemy] and update [Combat]", category: "Action", id: "07fdef4f8746f764f84440dad9c70999")]
public partial class ChooseEnemyAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Enemy;
    [SerializeReference] public BlackboardVariable<bool> Combat;
    [SerializeReference] public BlackboardVariable<float> Radius = new BlackboardVariable<float>(10f);
    [SerializeReference] public BlackboardVariable<float> HealthPercent;
    [SerializeReference] public BlackboardVariable<float> AttackDistance;
    [SerializeReference] public BlackboardVariable<float> RetreatUrgency;

    private readonly Collider2D[] _overlapBuffer = new Collider2D[32];

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        GameObject owner = GetOwner();
        if (owner == null)
            return Status.Failure;

        if (owner.TryGetComponent(out AllyAIBrain brain))
        {
            TargetSelectionResult result = brain.SelectWeaponTarget(forceRefresh: true);
            CombatSnapshot snapshot = brain.Snapshot;

            SyncBlackboard(brain);
            Combat.Value = snapshot.EnemyCount > 0;

            if (!result.HasTarget)
            {
                Enemy.Value = null;
                return Status.Failure;
            }

            Enemy.Value = result.Target.gameObject;
            return Status.Running;
        }

        return SelectNearestFallback(owner);
    }

    protected override void OnEnd()
    {
    }

    private Status SelectNearestFallback(GameObject owner)
    {
        int count = EnemyPhysics2D.OverlapCircle(owner.transform.position, Radius.Value, _overlapBuffer);
        SelectBestTarget(count, owner.transform.position, out Transform best, out bool anyInCombat);

        Combat.Value = anyInCombat;

        if (best == null)
        {
            Enemy.Value = null;
            return Status.Failure;
        }

        Enemy.Value = best.gameObject;
        return Status.Running;
    }

    private void SelectBestTarget(int count, Vector3 origin, out Transform best, out bool anyInCombat)
    {
        best = null;
        anyInCombat = false;
        float bestSq = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider2D c = _overlapBuffer[i];
            if (c == null) continue;
            if (!c.TryGetComponent(out EnemyDamage ed) || !ed.IsAlive) continue;

            if (ed.InCombat)
                anyInCombat = true;

            float sq = (c.transform.position - origin).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = c.transform;
            }
        }
    }

    private void SyncBlackboard(AllyAIBrain brain)
    {
        if (HealthPercent != null)
            HealthPercent.Value = brain.HealthPercent * 100f;

        if (AttackDistance != null)
            AttackDistance.Value = brain.AttackDistance;

        if (RetreatUrgency != null && brain.TryGetComponent(out AllyMLBridge mlBridge))
            RetreatUrgency.Value = mlBridge.RetreatUrgency * 100f;
    }

    private GameObject GetOwner()
    {
        return GameObject;
    }
}
