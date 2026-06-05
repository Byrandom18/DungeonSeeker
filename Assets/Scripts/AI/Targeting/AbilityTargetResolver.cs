using UnityEngine;

public static class AbilityTargetResolver
{
    public static bool TryResolve(
        AbilitySO data,
        CombatSnapshot snapshot,
        out Vector3 aimPosition,
        out Transform target)
    {
        aimPosition = snapshot.Position;
        target = null;

        if (data == null)
            return false;

        switch (data.TargetType)
        {
            case AbilityTargetType.Enemy:
                return TryResolveEnemy(snapshot, data, out aimPosition, out target);

            case AbilityTargetType.Ally:
                return TryResolveAlly(snapshot, data, out aimPosition, out target);

            case AbilityTargetType.Self:
                aimPosition = snapshot.Position;
                target = null;
                return true;

            case AbilityTargetType.Ground:
                return TryResolveGround(snapshot, data, out aimPosition, out target);

            case AbilityTargetType.Direction:
                return TryResolveDirection(snapshot, out aimPosition, out target);

            default:
                return TryResolveEnemy(snapshot, data, out aimPosition, out target);
        }
    }

    private static bool TryResolveEnemy(
        CombatSnapshot snapshot,
        AbilitySO data,
        out Vector3 aimPosition,
        out Transform target)
    {
        aimPosition = snapshot.Position;
        target = null;

        if (snapshot.Enemies == null || snapshot.Enemies.Length == 0)
            return false;

        float bestScore = float.MinValue;

        foreach (EnemySnapshot enemy in snapshot.Enemies)
        {
            if (enemy.Transform == null) continue;
            if (enemy.Distance > data.MaxRange) continue;
            if (data.MinRange > 0f && enemy.Distance < data.MinRange) continue;

            float score = 1f - enemy.HealthPercent;
            if (enemy.InCombat) score += 0.2f;

            if (score > bestScore)
            {
                bestScore = score;
                target = enemy.Transform;
                aimPosition = enemy.Transform.position;
            }
        }

        return target != null;
    }

    private static bool TryResolveAlly(
        CombatSnapshot snapshot,
        AbilitySO data,
        out Vector3 aimPosition,
        out Transform target)
    {
        aimPosition = snapshot.Position;
        target = null;

        if (snapshot.Allies == null || snapshot.Allies.Length == 0)
            return false;

        float lowestHealth = float.MaxValue;

        foreach (AllySnapshot ally in snapshot.Allies)
        {
            if (ally.Transform == null || ally.IsSelf) continue;
            if (ally.Distance > data.MaxRange) continue;
            if (ally.HealthPercent >= data.AllyHealThreshold) continue;

            if (ally.HealthPercent < lowestHealth)
            {
                lowestHealth = ally.HealthPercent;
                target = ally.Transform;
                aimPosition = ally.Transform.position;
            }
        }

        if (target != null)
            return true;

        if (snapshot.HealthPercent < data.AllyHealThreshold)
        {
            aimPosition = snapshot.Position;
            return true;
        }

        return false;
    }

    private static bool TryResolveGround(
        CombatSnapshot snapshot,
        AbilitySO data,
        out Vector3 aimPosition,
        out Transform target)
    {
        aimPosition = snapshot.Position;
        target = null;

        if (snapshot.Enemies == null || snapshot.Enemies.Length == 0)
            return false;

        Vector2 centroid = Vector2.zero;
        int count = 0;

        foreach (EnemySnapshot enemy in snapshot.Enemies)
        {
            if (enemy.Distance > data.MaxRange) continue;
            centroid += (Vector2)enemy.Transform.position;
            count++;
        }

        if (count == 0)
            return false;

        aimPosition = centroid / count;
        aimPosition.z = snapshot.Position.z;
        return true;
    }

    private static bool TryResolveDirection(
        CombatSnapshot snapshot,
        out Vector3 aimPosition,
        out Transform target)
    {
        target = null;
        aimPosition = snapshot.Position;

        if (snapshot.Enemies == null || snapshot.Enemies.Length == 0)
            return false;

        float nearest = float.MaxValue;
        Vector3 nearestPos = snapshot.Position;

        foreach (EnemySnapshot enemy in snapshot.Enemies)
        {
            if (enemy.Distance < nearest)
            {
                nearest = enemy.Distance;
                nearestPos = enemy.Transform.position;
            }
        }

        aimPosition = nearestPos;
        return true;
    }
}
