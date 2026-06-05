using UnityEngine;

[CreateAssetMenu(fileName = "AbilityScoreWeights", menuName = "AI/Ability Score Weights")]
public class AbilityScoreWeights : ScriptableObject
{
    [Range(0f, 1f)] public float MinScoreToCast = 0.35f;

    [Header("Tag multipliers")]
    [Range(0f, 2f)] public float SingleTarget = 1f;
    [Range(0f, 2f)] public float MultiTarget = 1.2f;
    [Range(0f, 2f)] public float Defence = 1f;
    [Range(0f, 2f)] public float Support = 0.9f;
    [Range(0f, 2f)] public float Heal = 1.1f;

    public float GetTagMultiplier(AbilityBehaviorTag tag)
    {
        return tag switch
        {
            AbilityBehaviorTag.SingleTarget => SingleTarget,
            AbilityBehaviorTag.MultiTarget => MultiTarget,
            AbilityBehaviorTag.Defence => Defence,
            AbilityBehaviorTag.Support => Support,
            AbilityBehaviorTag.Heal => Heal,
            _ => 1f
        };
    }
}
