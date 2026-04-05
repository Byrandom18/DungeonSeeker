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

/// <summary>The main item stat: type + base value + level scale.</summary>
[Serializable]
public struct MainStatDefinition
{
    public StatType Type;
    public float BaseValue;
    public float ValueScalePerLevel;   // прибавляется каждый уровень улучшения
}

/// <summary>pool in ItemSO.</summary>
[Serializable]
public struct BonusStatDefinition
{
    public StatType Type;
    public float MinValue;        // минимальное значение при выпадении
    public float MaxValue;        // максимальное значение при выпадении
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