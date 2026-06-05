#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class AllyAIProfileFactory
{
    private const string ProfilesFolder = "Assets/Scripts/AI/Profiles";

    [MenuItem("AI/Create Default Ally Profiles")]
    public static void CreateDefaultProfiles()
    {
        EnsureFolder(ProfilesFolder);

        CreateProfileSet(AllyCombatProfileType.Aggressive, ProfilesFolder);
        CreateProfileSet(AllyCombatProfileType.Defensive, ProfilesFolder);
        CreateProfileSet(AllyCombatProfileType.Support, ProfilesFolder);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created default Ally AI profiles in " + ProfilesFolder);
    }

    private static void CreateProfileSet(AllyCombatProfileType type, string folder)
    {
        string prefix = type.ToString();

        var weaponWeights = ScriptableObject.CreateInstance<TargetScoreWeights>();
        var abilityWeights = ScriptableObject.CreateInstance<AbilityScoreWeights>();
        var profile = AllyAIProfile.CreateRuntimeDefault(type);

        ApplySerializedDefaults(type, weaponWeights, abilityWeights);

        AssetDatabase.CreateAsset(weaponWeights, $"{folder}/{prefix}WeaponWeights.asset");
        AssetDatabase.CreateAsset(abilityWeights, $"{folder}/{prefix}AbilityWeights.asset");

        profile.WeaponTargetWeights = weaponWeights;
        profile.AbilityScoreWeights = abilityWeights;

        AssetDatabase.CreateAsset(profile, $"{folder}/{prefix}Profile.asset");
    }

    private static void ApplySerializedDefaults(
        AllyCombatProfileType type,
        TargetScoreWeights weapon,
        AbilityScoreWeights ability)
    {
        switch (type)
        {
            case AllyCombatProfileType.Aggressive:
                ability.MinScoreToCast = 0.3f;
                ability.SingleTarget = 1.2f;
                ability.Heal = 0.5f;
                ability.Defence = 0.4f;
                weapon.LowHealthWeight = 1f;
                weapon.ThreatWeight = 0.8f;
                break;
            case AllyCombatProfileType.Defensive:
                ability.MinScoreToCast = 0.4f;
                ability.Defence = 1.4f;
                ability.Heal = 0.8f;
                ability.SingleTarget = 0.7f;
                weapon.ThreatWeight = 1f;
                weapon.DistanceWeight = 1.2f;
                break;
            case AllyCombatProfileType.Support:
                ability.MinScoreToCast = 0.35f;
                ability.Heal = 1.5f;
                ability.Support = 1.2f;
                ability.Defence = 1f;
                ability.SingleTarget = 0.6f;
                weapon.LowHealthWeight = 0.5f;
                weapon.FocusFireWeight = 0.2f;
                break;
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
