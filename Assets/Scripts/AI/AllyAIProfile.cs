using UnityEngine;

public enum AllyCombatProfileType
{
    Aggressive,
    Defensive,
    Support
}

[CreateAssetMenu(fileName = "AllyAIProfile", menuName = "AI/Ally AI Profile")]
public class AllyAIProfile : ScriptableObject
{
    public AllyCombatProfileType ProfileType = AllyCombatProfileType.Aggressive;

    [Header("Decision weights")]
    public TargetScoreWeights WeaponTargetWeights;
    public AbilityScoreWeights AbilityScoreWeights;

    [Header("Combat pacing")]
    [Range(0f, 1f)] public float MinAbilityScoreOverride = -1f;
    [Tooltip("Multiplier applied to Attack Distance for kiting (1 = weapon range, 0.5 = closer).")]
    [Range(0.3f, 1.5f)] public float PreferredCombatDistanceScale = 1f;
    [Range(0f, 1f)] public float RetreatHealthThreshold = 0.2f;

    [Header("ML tuning defaults")]
    [Range(0f, 1f)] public float DefaultPreferredDistance = 0.6f;
    [Range(0f, 1f)] public float DefaultRetreatUrgency;

    public void ApplyDefaultsTo(AllyAIBrain brain)
    {
        if (brain == null) return;
        brain.ApplyProfile(this);
    }

    public static AllyAIProfile CreateRuntimeDefault(AllyCombatProfileType type)
    {
        var profile = CreateInstance<AllyAIProfile>();
        profile.ProfileType = type;

        profile.WeaponTargetWeights = CreateInstance<TargetScoreWeights>();
        profile.AbilityScoreWeights = CreateInstance<AbilityScoreWeights>();

        switch (type)
        {
            case AllyCombatProfileType.Aggressive:
                profile.PreferredCombatDistanceScale = 0.85f;
                profile.AbilityScoreWeights.MinScoreToCast = 0.3f;
                profile.AbilityScoreWeights.SingleTarget = 1.2f;
                profile.AbilityScoreWeights.MultiTarget = 1f;
                profile.AbilityScoreWeights.Heal = 0.5f;
                profile.AbilityScoreWeights.Defence = 0.4f;
                profile.WeaponTargetWeights.LowHealthWeight = 1f;
                profile.WeaponTargetWeights.ThreatWeight = 0.8f;
                break;

            case AllyCombatProfileType.Defensive:
                profile.PreferredCombatDistanceScale = 1.15f;
                profile.RetreatHealthThreshold = 0.3f;
                profile.AbilityScoreWeights.MinScoreToCast = 0.4f;
                profile.AbilityScoreWeights.Defence = 1.4f;
                profile.AbilityScoreWeights.Heal = 0.8f;
                profile.AbilityScoreWeights.SingleTarget = 0.7f;
                profile.WeaponTargetWeights.ThreatWeight = 1f;
                profile.WeaponTargetWeights.DistanceWeight = 1.2f;
                break;

            case AllyCombatProfileType.Support:
                profile.PreferredCombatDistanceScale = 1.1f;
                profile.AbilityScoreWeights.MinScoreToCast = 0.35f;
                profile.AbilityScoreWeights.Heal = 1.5f;
                profile.AbilityScoreWeights.Support = 1.2f;
                profile.AbilityScoreWeights.Defence = 1f;
                profile.AbilityScoreWeights.SingleTarget = 0.6f;
                profile.WeaponTargetWeights.LowHealthWeight = 0.5f;
                profile.WeaponTargetWeights.FocusFireWeight = 0.2f;
                break;
        }

        return profile;
    }
}
