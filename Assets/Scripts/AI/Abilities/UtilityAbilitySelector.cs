using UnityEngine;


public class UtilityAbilitySelector
{
    private readonly AbilityScoreWeights _weights;

    public UtilityAbilitySelector(AbilityScoreWeights weights = null)
    {
        _weights = weights != null
            ? weights
            : ScriptableObject.CreateInstance<AbilityScoreWeights>();
    }

    public float MinScoreToCast => _weights.MinScoreToCast;

    public AbilityDecision Select(CombatSnapshot snapshot, AbilitySystem abilitySystem, ActiveWeapon activeWeapon)
    {
        if (abilitySystem == null)
            return default;

        float bestScore = 0f;
        AbilityDecision best = default;

        int slotCount = abilitySystem.AbilitySlotCount;
        for (int i = 0; i < slotCount; i++)
        {
            AbilitySO data = abilitySystem.GetAbilityData(i);
            if (data == null) continue;

            if (!abilitySystem.CanUseSlot(i, activeWeapon))
                continue;

            if (!AbilityTargetResolver.TryResolve(data, snapshot, out Vector3 aim, out Transform target))
                continue;

            float score = EvaluateSlotScore(data, snapshot, aim, target);
            if (score <= bestScore)
                continue;

            bestScore = score;
            best = new AbilityDecision
            {
                SlotIndex = i,
                AimPosition = aim,
                Target = target,
                Score = score
            };
        }

        if (bestScore < _weights.MinScoreToCast)
            return default;

        return best;
    }

    private float EvaluateSlotScore(
        AbilitySO data,
        CombatSnapshot snapshot,
        Vector3 aim,
        Transform target)
    {
        float situational = EvaluateSituationalScore(data, snapshot);
        float rangeFactor = EvaluateRangeFactor(data, snapshot, aim);
        float manaFactor = EvaluateManaFactor(data, snapshot);

        return situational * rangeFactor * manaFactor * _weights.GetTagMultiplier(data.BehaviorTag);
    }

    private static float EvaluateSituationalScore(AbilitySO data, CombatSnapshot snapshot)
    {
        switch (data.BehaviorTag)
        {
            case AbilityBehaviorTag.SingleTarget:
                if (snapshot.EnemyCount <= 0) return 0f;
                return snapshot.EnemyCount <= 2 ? 1f : 0.6f;

            case AbilityBehaviorTag.MultiTarget:
                if (snapshot.ClusteredEnemyCount < data.MinEnemiesForAoE) return 0f;
                return Mathf.Clamp01(snapshot.ClusteredEnemyCount / (float)data.MinEnemiesForAoE);

            case AbilityBehaviorTag.Heal:
                if (!snapshot.AnyAllyLowHealth && snapshot.HealthPercent >= data.AllyHealThreshold)
                    return 0f;
                return snapshot.AnyAllyLowHealth ? 1f : 1f - snapshot.HealthPercent;

            case AbilityBehaviorTag.Defence:
                if (snapshot.HealthPercent > data.SelfDefenceThreshold && snapshot.EnemyCount < 3)
                    return 0f;
                return 1f - snapshot.HealthPercent + snapshot.EnemyCount * 0.1f;

            case AbilityBehaviorTag.Support:
                return snapshot.AnyEnemyInCombat ? 0.8f : 0.3f;

            default:
                return snapshot.EnemyCount > 0 ? 0.7f : 0f;
        }
    }

    private static float EvaluateRangeFactor(AbilitySO data, CombatSnapshot snapshot, Vector3 aim)
    {
        if (data.TargetType == AbilityTargetType.Self)
            return 1f;

        float dist = Vector2.Distance(snapshot.Position, aim);
        if (data.MaxRange <= 0f)
            return 1f;

        if (dist > data.MaxRange)
            return 0f;

        if (data.MinRange > 0f && dist < data.MinRange)
            return 0.3f;

        float optimal = data.OptimalRange > 0f ? data.OptimalRange : data.MaxRange * 0.7f;
        float delta = Mathf.Abs(dist - optimal);
        return 1f - Mathf.Clamp01(delta / Mathf.Max(optimal, 0.5f)) * 0.5f;
    }

    private static float EvaluateManaFactor(AbilitySO data, CombatSnapshot snapshot)
    {
        if (data.ManaCost <= 0f)
            return 1f;

        if (snapshot.ManaPercent < 0.2f)
            return 0.4f;

        return Mathf.Clamp01(snapshot.ManaPercent + 0.15f);
    }
}
