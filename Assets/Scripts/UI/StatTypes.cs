using System;
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
    CritChance,
    CritDamage,
    SizeMod,
    ManaFlat,
    ManaRegenMod,
    SpellDamageMod,
    BaseAttackDamageMod,
    BaseAttackSpeedMod,
    CooldownReduction,
    Luck
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

[Serializable]
public struct BonusStatEntry
{
    public StatType Type;
    public float MinValue;
    public float MaxValue;
    public float Weight;
}

[Serializable]
public struct MainStatInstance
{
    public StatType Type;
    public float BaseValue;
    public float ValueScalePerLevel;

    public float GetValue(int upgradeLevel) => BaseValue + BaseValue * ValueScalePerLevel * upgradeLevel;
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