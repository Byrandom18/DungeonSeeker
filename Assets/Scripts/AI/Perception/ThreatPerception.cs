using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ThreatPerception : MonoBehaviour
{
    private struct ThreatEntry
    {
        public Vector2 EscapeDirection;
        public float Urgency;
        public float Distance;
        public float TimeToImpact;
    }

    [SerializeField] private CombatPerception _combatPerception;
    [SerializeField] private float _threatScanRadius = 10f;
    [SerializeField] private float _threatAlignmentThreshold = 0.55f;
    [SerializeField] private float _maxThreatTime = 2f;
    [SerializeField] private float _meleeThreatRadius = 4f;
    [SerializeField] private int _maxThreatEntries = 4;

    private readonly List<ThreatEntry> _threatBuffer = new List<ThreatEntry>(8);

    public ThreatSnapshot LastSnapshot { get; private set; }

    private void Awake()
    {
        if (_combatPerception == null)
            _combatPerception = GetComponent<CombatPerception>();
    }

    public ThreatSnapshot BuildSnapshot(Vector3 position, CombatSnapshot combat)
    {
        _threatBuffer.Clear();

        ThreatSnapshot snapshot = default;
        snapshot.AttackingEnemyCount = combat.AttackingEnemyCount;
        snapshot.AttackingSelfCount = combat.AttackingSelfCount;
        snapshot.AnyEnemyAttackingSelf = combat.AttackingSelfCount > 0;
        snapshot.NearestEnemyAttacking = combat.AttackingEnemyCount > 0;
        snapshot.NearestEnemyBearing = GetNearestAttackingSelfBearing(position, combat);

        EvaluateAttackingEnemies(position, combat, ref snapshot);
        EvaluateIncomingProjectiles(position);
        ApplyAggregatedThreat(ref snapshot);

        LastSnapshot = snapshot;
        return snapshot;
    }

    private static float GetNearestAttackingSelfBearing(Vector3 position, CombatSnapshot combat)
    {
        if (combat.Enemies == null || combat.EnemyCount == 0)
            return 0.5f;

        float nearest = float.MaxValue;
        Vector2 toEnemy = Vector2.right;

        for (int i = 0; i < combat.EnemyCount; i++)
        {
            EnemySnapshot enemy = combat.Enemies[i];
            if (enemy.Transform == null || !enemy.IsAttackingSelf || enemy.Distance >= nearest)
                continue;

            nearest = enemy.Distance;
            toEnemy = (Vector2)(enemy.Transform.position - position);
        }

        if (nearest == float.MaxValue)
            return GetNearestEnemyBearing(position, combat);

        if (toEnemy.sqrMagnitude < 0.0001f)
            return 0.5f;

        float angle = Mathf.Atan2(toEnemy.y, toEnemy.x);
        return Mathf.Repeat(angle / (Mathf.PI * 2f) + 0.5f, 1f);
    }

    private static float GetNearestEnemyBearing(Vector3 position, CombatSnapshot combat)
    {
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

        if (nearest == float.MaxValue || toEnemy.sqrMagnitude < 0.0001f)
            return 0.5f;

        float angle = Mathf.Atan2(toEnemy.y, toEnemy.x);
        return Mathf.Repeat(angle / (Mathf.PI * 2f) + 0.5f, 1f);
    }

    private void EvaluateAttackingEnemies(Vector3 position, CombatSnapshot combat, ref ThreatSnapshot snapshot)
    {
        if (combat.Enemies == null)
            return;

        Vector2 allyPos = position;
        float maxSelfUrgency = 0f;

        for (int i = 0; i < combat.EnemyCount; i++)
        {
            EnemySnapshot enemy = combat.Enemies[i];
            if (enemy.Transform == null || !enemy.IsAttacking)
                continue;
            if (enemy.Distance > _meleeThreatRadius)
                continue;

            Vector2 awayFromAttacker = allyPos - (Vector2)enemy.Transform.position;
            if (awayFromAttacker.sqrMagnitude < 0.0001f)
                awayFromAttacker = Vector2.right;
            awayFromAttacker.Normalize();

            float proximity = 1f - Mathf.Clamp01(enemy.Distance / _meleeThreatRadius);
            float urgency = enemy.IsAttackingSelf
                ? 0.65f + proximity * 0.35f
                : 0.35f + proximity * 0.25f;

            if (enemy.IsAttackingSelf)
                maxSelfUrgency = Mathf.Max(maxSelfUrgency, urgency);

            AddThreat(awayFromAttacker, urgency, enemy.Distance, 0f);
        }

        snapshot.AttackingSelfUrgency = maxSelfUrgency;
    }

    private void EvaluateIncomingProjectiles(Vector3 position)
    {
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
            Vector2 lateral = new Vector2(-moveDir.y, moveDir.x);
            if (Vector2.Dot(lateral, toAlly) < 0f)
                lateral = -lateral;

            AddThreat(lateral, urgency, distance, timeToImpact);
        }
    }

    private void AddThreat(Vector2 escapeDirection, float urgency, float distance, float timeToImpact)
    {
        if (escapeDirection.sqrMagnitude < 0.0001f || urgency <= 0f)
            return;

        _threatBuffer.Add(new ThreatEntry
        {
            EscapeDirection = escapeDirection.normalized,
            Urgency = urgency,
            Distance = distance,
            TimeToImpact = timeToImpact
        });
    }

    private void ApplyAggregatedThreat(ref ThreatSnapshot snapshot)
    {
        _threatBuffer.Sort((a, b) => b.Urgency.CompareTo(a.Urgency));

        int count = Mathf.Min(_threatBuffer.Count, _maxThreatEntries);
        Vector2 weightedEscape = Vector2.zero;
        float bestUrgency = 0f;
        float bestDistance = float.MaxValue;
        float bestTime = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            ThreatEntry entry = _threatBuffer[i];
            weightedEscape += entry.EscapeDirection * entry.Urgency;
            bestUrgency = Mathf.Max(bestUrgency, entry.Urgency);
            bestDistance = Mathf.Min(bestDistance, entry.Distance);
            if (entry.TimeToImpact > 0f)
                bestTime = Mathf.Min(bestTime, entry.TimeToImpact);
        }

        _threatBuffer.Clear();

        float meleeUrgency = snapshot.AttackingSelfUrgency;
        if (meleeUrgency > bestUrgency)
            bestUrgency = meleeUrgency;

        snapshot.HasIncomingThreat = bestUrgency > 0f;
        snapshot.IncomingThreatUrgency = bestUrgency;
        snapshot.NearestThreatDistance = bestDistance == float.MaxValue ? -1f : bestDistance;
        snapshot.NearestThreatTimeToImpact = bestTime == float.MaxValue ? -1f : bestTime;
        snapshot.ThreatDirection = weightedEscape.sqrMagnitude > 0.0001f
            ? weightedEscape.normalized
            : Vector2.zero;
    }
}
