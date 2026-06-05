using UnityEngine;

public static class WeaponTargetSelector
{
    private static TargetScoreWeights _defaultWeights;

    public static TargetSelectionResult Select(CombatSnapshot snapshot, TargetScoreWeights weights = null)
    {
        if (snapshot.Enemies == null || snapshot.Enemies.Length == 0)
            return default;

        weights ??= GetDefaultWeights();
        WeaponWeightProfile profile = weights.GetProfile(snapshot.EquippedWeapon);

        float bestScore = float.MinValue;
        Transform bestTarget = null;

        float maxDist = snapshot.WeaponRange > 0f
            ? snapshot.WeaponRange * 1.5f
            : 12f;

        foreach (EnemySnapshot enemy in snapshot.Enemies)
        {
            if (enemy.Transform == null) continue;

            float score = ScoreEnemy(enemy, snapshot, weights, profile, maxDist);
            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = enemy.Transform;
            }
        }

        return new TargetSelectionResult
        {
            Target = bestTarget,
            Score = bestScore
        };
    }

    private static float ScoreEnemy(
        EnemySnapshot enemy,
        CombatSnapshot snapshot,
        TargetScoreWeights weights,
        WeaponWeightProfile profile,
        float maxDist)
    {
        float distanceScore = EvaluateDistanceScore(
            enemy.Distance,
            snapshot.OptimalWeaponRange,
            maxDist,
            profile.PreferCloser);

        float lowHealthScore = 1f - Mathf.Clamp01(enemy.HealthPercent);
        float threatScore = enemy.InCombat ? 1f : 0f;
        float focusScore = Mathf.Clamp01(enemy.FocusFireCount / 3f);

        float score =
            weights.DistanceWeight * profile.DistanceWeightScale * distanceScore +
            weights.LowHealthWeight * profile.LowHealthWeightScale * lowHealthScore +
            weights.ThreatWeight * profile.ThreatWeightScale * threatScore +
            weights.FocusFireWeight * focusScore;

        return score;
    }

    private static float EvaluateDistanceScore(
        float distance,
        float optimalRange,
        float maxDist,
        float preferCloser)
    {
        if (maxDist <= 0f)
            return 0f;

        if (preferCloser >= 0.9f)
            return 1f - Mathf.Clamp01(distance / maxDist);

        if (preferCloser <= 0.1f)
            return Mathf.Clamp01(distance / maxDist);

        float range = Mathf.Max(optimalRange, 0.5f);
        float delta = Mathf.Abs(distance - range);
        return 1f - Mathf.Clamp01(delta / range);
    }

    private static TargetScoreWeights GetDefaultWeights()
    {
        if (_defaultWeights != null)
            return _defaultWeights;

        _defaultWeights = ScriptableObject.CreateInstance<TargetScoreWeights>();
        return _defaultWeights;
    }
}
