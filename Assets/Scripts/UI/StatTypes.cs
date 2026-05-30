using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatType
{
    AttackMod,
    AttackFlat,
    HealthMod,
    HealthFlat,
    DefenceMod,
    DefenceFlat,
    Resistance,
    CritRate,
    CritDamage,
    SizeMod,
    ManaFlat,
    ManaRegenMod,
    SpellDamageMod,
    BaseAttackDamageMod,
    BaseAttackSpeedMod,
    CooldownReduction,
    Luck,
    Speed
}

[Serializable]
public struct MainStatEntry
{
    public StatType Type;
    public float BaseValue;
    //public float MinBaseValue;
    //public float MaxBaseValue;
    public float ValueScalePerLevel;
    public float Weight;
}

//[Serializable]
//public struct BonusStatEntry
//{
//    public StatType Type;
//    public float MinValue;
//    public float MaxValue;
//    public float Weight;
//}

[Serializable]
public struct BonusOption
{
    [Range(0f, 100f)]
    public float Weight;
    public float Value;
}

[Serializable]
public struct StatBonusPoolEntry
{
    [Header("Stats")]
    public StatType Type;
    [Range(0f, 100f)]
    public float TypeWeight;
    public List<BonusOption> Options;  
}

[Serializable]
public struct MainStatInstance
{
    public StatType Type;
    public float BaseValue;
    public float ValueScalePerLevel;

    public float GetValue(int upgradeLevel) => BaseValue * Mathf.Pow(1 + ValueScalePerLevel, upgradeLevel);
}

/// <summary>exited stat storing in InventoryItemData.</summary>
[Serializable]
public struct BonusStatInstance
{
    public StatType Type;
    public float Value;

    public BonusStatInstance(StatType type, float value)
    {
        Type = type;
        Value = value;
    }
}

public static class RarityStatMultiplier
{
    private static readonly Dictionary<ItemRarity, float> _multipliers = new()
    {
        { ItemRarity.Common,    0.5f },
        { ItemRarity.Uncommon,  0.6f },
        { ItemRarity.Rare,      0.7f },
        { ItemRarity.Epic,      0.8f },
        { ItemRarity.Legendary, 0.9f },
        { ItemRarity.Unique,    1.0f }
    };

    public static float Get(ItemRarity rarity) =>
        _multipliers.TryGetValue(rarity, out var m) ? m : 1f;
}