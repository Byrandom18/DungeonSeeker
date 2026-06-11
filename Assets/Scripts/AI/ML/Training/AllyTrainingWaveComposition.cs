using UnityEngine;

/// <summary>
/// Picks enemy prefab indices per spawn slot for training curriculum waves.
/// Prefab 0 = melee, prefab 1 = ranged (requires 2+ entries in spawner array).
/// </summary>
public static class AllyTrainingWaveComposition
{
    public static void GetWaveCounts(int curriculumLevel, out int meleeCount, out int rangedCount)
    {
        switch (Mathf.Max(0, curriculumLevel))
        {
            case 0:
                meleeCount = 0;
                rangedCount = 1;
                break;
            case 1:
                meleeCount = 1;
                rangedCount = 1;
                break;
            case 2:
                meleeCount = 1;
                rangedCount = 2;
                break;
            case 3:
                meleeCount = 1;
                rangedCount = 3;
                break;
            default:
                meleeCount = 2;
                rangedCount = 3;
                break;
        }
    }

    public static int GetTotalCount(int curriculumLevel)
    {
        GetWaveCounts(curriculumLevel, out int melee, out int ranged);
        return melee + ranged;
    }

    public static int GetPrefabIndex(int curriculumLevel, int slotIndex, int totalCount, int prefabCount)
    {
        if (prefabCount <= 0)
            return 0;

        if (prefabCount == 1)
            return 0;

        GetWaveCounts(curriculumLevel, out int meleeCount, out int rangedCount);

        int meleeIndex = 0;
        int rangedIndex = Mathf.Min(1, prefabCount - 1);

        if (slotIndex < meleeCount)
            return meleeIndex;

        if (slotIndex < meleeCount + rangedCount)
            return rangedIndex;

        return slotIndex % 2 == 0 ? meleeIndex : rangedIndex;
    }
}
