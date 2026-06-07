using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ThreatPerception : MonoBehaviour
{
    [SerializeField] private CombatPerception _combatPerception;
    [SerializeField] private float _threatScanRadius = 10f;
    [SerializeField] private float _threatAlignmentThreshold = 0.55f;
    [SerializeField] private float _maxThreatTime = 2f;

    public ThreatSnapshot LastSnapshot { get; private set; }

    private void Awake()
    {
        if (_combatPerception == null)
            _combatPerception = GetComponent<CombatPerception>();
    }

    public ThreatSnapshot BuildSnapshot(Vector3 position, CombatSnapshot combat)
    {
        ThreatSnapshot snapshot = default;
        snapshot.NearestEnemyAttacking = IsNearestEnemyAttacking(combat);
        snapshot.NearestEnemyBearing = GetNearestEnemyBearing(position, combat);
        EvaluateIncomingThreats(position, ref snapshot);
        LastSnapshot = snapshot;
        return snapshot;
    }

    private static bool IsNearestEnemyAttacking(CombatSnapshot combat)
    {
        if (combat.Enemies == null || combat.EnemyCount == 0)
            return false;

        float nearest = float.MaxValue;
        bool attacking = false;

        for (int i = 0; i < combat.EnemyCount; i++)
        {
            EnemySnapshot enemy = combat.Enemies[i];
            if (enemy.Transform == null || enemy.Distance >= nearest)
                continue;

            nearest = enemy.Distance;
            attacking = enemy.Transform.TryGetComponent(out EnemyAI ai) && ai.IsAttacking;
        }

        return attacking;
    }

    private static float GetNearestEnemyBearing(Vector3 position, CombatSnapshot combat)
    {
        if (combat.Enemies == null || combat.EnemyCount == 0)
            return 0.5f;

        float nearest = float.MaxValue;
        Vector2 toEnemy = Vector2.right;

        for (int i = 0; i < combat.EnemyCount; i++)
        {
            EnemySnapshot enemy = combat.Enemies[i];
            if (enemy.Transform == null || enemy.Distance >= nearest)
                continue;

            nearest = enemy.Distance;
            toEnemy = (Vector2)(enemy.Transform.position - position);
        }

        if (toEnemy.sqrMagnitude < 0.0001f)
            return 0.5f;

        float angle = Mathf.Atan2(toEnemy.y, toEnemy.x);
        return Mathf.Repeat(angle / (Mathf.PI * 2f) + 0.5f, 1f);
    }

    private void EvaluateIncomingThreats(Vector3 position, ref ThreatSnapshot snapshot)
    {
        float bestUrgency = 0f;
        float bestDistance = float.MaxValue;
        float bestTime = float.MaxValue;
        Vector2 bestDirection = Vector2.zero;

        IReadOnlyList<Projectile> projectiles = EnemyProjectileRegistry.ActiveProjectiles;
        Vector2 allyPos = position;

        for (int i = 0; i < projectiles.Count; i++)
        {
            Projectile projectile = projectiles[i];
            if (projectile == null || !projectile.EnemyLaunch)
                continue;

            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb == null)
                continue;

            Vector2 projectilePos = projectile.transform.position;
            float distance = Vector2.Distance(allyPos, projectilePos);
            if (distance > _threatScanRadius)
                continue;

            Vector2 velocity = rb.linearVelocity;
            float speed = velocity.magnitude;
            if (speed < 0.01f)
                continue;

            Vector2 toAlly = allyPos - projectilePos;
            if (toAlly.sqrMagnitude < 0.0001f)
                continue;

            Vector2 moveDir = velocity / speed;
            float alignment = Vector2.Dot(moveDir, toAlly.normalized);
            if (alignment < _threatAlignmentThreshold)
                continue;

            float timeToImpact = distance / speed;
            if (timeToImpact > _maxThreatTime)
                continue;

            float urgency = Mathf.Clamp01(1f - timeToImpact / _maxThreatTime);
            if (urgency < bestUrgency && bestUrgency > 0f)
                continue;

            bestUrgency = urgency;
            bestDistance = distance;
            bestTime = timeToImpact;
            bestDirection = moveDir;
        }

        snapshot.HasIncomingThreat = bestUrgency > 0f;
        snapshot.IncomingThreatUrgency = bestUrgency;
        snapshot.NearestThreatDistance = bestDistance == float.MaxValue ? -1f : bestDistance;
        snapshot.NearestThreatTimeToImpact = bestTime == float.MaxValue ? -1f : bestTime;
        snapshot.ThreatDirection = bestDirection;
    }
}
