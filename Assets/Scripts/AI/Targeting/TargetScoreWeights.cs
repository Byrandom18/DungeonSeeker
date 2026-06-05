using UnityEngine;

[CreateAssetMenu(fileName = "TargetScoreWeights", menuName = "AI/Target Score Weights")]
public class TargetScoreWeights : ScriptableObject
{
    [Header("Global")]
    [Range(0f, 2f)] public float DistanceWeight = 1f;
    [Range(0f, 2f)] public float LowHealthWeight = 0.8f;
    [Range(0f, 2f)] public float ThreatWeight = 0.6f;
    [Range(0f, 2f)] public float FocusFireWeight = 0.4f;

    [Header("Per weapon type")]
    public WeaponWeightProfile Sword = WeaponWeightProfile.DefaultMelee();
    public WeaponWeightProfile Bow = WeaponWeightProfile.DefaultRanged();
    public WeaponWeightProfile Staff = WeaponWeightProfile.DefaultRanged();
    public WeaponWeightProfile Talisman = WeaponWeightProfile.DefaultRanged();

    public WeaponWeightProfile GetProfile(WeaponType type)
    {
        return type switch
        {
            WeaponType.Sword => Sword,
            WeaponType.Bow => Bow,
            WeaponType.Staff => Staff,
            WeaponType.Talisman => Talisman,
            _ => Sword
        };
    }
}

[System.Serializable]
public struct WeaponWeightProfile
{
    [Tooltip("1 = prefer closer targets, 0 = prefer farther targets, 0.5 = bell curve around optimal range")]
    [Range(0f, 1f)] public float PreferCloser;

    [Range(0f, 2f)] public float DistanceWeightScale;
    [Range(0f, 2f)] public float LowHealthWeightScale;
    [Range(0f, 2f)] public float ThreatWeightScale;

    public static WeaponWeightProfile DefaultMelee()
    {
        return new WeaponWeightProfile
        {
            PreferCloser = 1f,
            DistanceWeightScale = 1.2f,
            LowHealthWeightScale = 0.7f,
            ThreatWeightScale = 1f
        };
    }

    public static WeaponWeightProfile DefaultRanged()
    {
        return new WeaponWeightProfile
        {
            PreferCloser = 0.5f,
            DistanceWeightScale = 1f,
            LowHealthWeightScale = 1f,
            ThreatWeightScale = 0.7f
        };
    }
}
