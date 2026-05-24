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

        int count = GatherEnemyColliders(owner.transform.position);
        SelectBestTarget(count, owner.transform.position, out Transform best, out bool anyInCombat);

        Combat.Value = anyInCombat;

        if (best == null)
        {
            Enemy.Value = null;
            return Status.Failure;
        }

        Enemy.Value = best.gameObject;

        return Status.Success;
    }

    protected override void OnEnd()
    {
    }

    private int GatherEnemyColliders(Vector2 center)
    {
        return EnemyPhysics2D.OverlapCircle(center, Radius.Value, _overlapBuffer);
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

    private GameObject GetOwner()
    {
        return GameObject;
    }
}

