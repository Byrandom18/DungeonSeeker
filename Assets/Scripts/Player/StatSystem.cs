using System;
using System.Collections.Generic;


/// <summary>
/// Modifier source — defines the group to be deleted en masse.
/// For example, when removing all equipment, we delete only the Equipment group,
/// the talents and buffs of the race remain intact.
/// <summary>
public enum ModifierSource
{
    Equipment,
    Talent,
    RunBuff
}


[Serializable]
public struct StatModifier
{
    public StatType Type;
    public float Value;
    public ModifierSource Source;
    public bool IsPercent; // false = flat, true = percent

    public StatModifier(StatType type, float value, ModifierSource source, bool isPercent = false)
    {
        Type = type;
        Value = value;
        Source = source;
        IsPercent = isPercent;
    }
}

/// <summary>
/// Stores basic stats values and modifier lists from three sources.
/// The final value formula:
///   final = (base + sumFlat) * (1 + sumPercent)
///
/// Usage:
///   float atk = _statSystem.GetFinalValue(StatType.AttackFlat);
/// </summary>
public class StatSystem
{
    // Basic values (from character class / initial values)
    private readonly Dictionary<StatType, float> _baseValues = new Dictionary<StatType, float>();

    // Modifiers by source — three separate lists for quick
    // mass removal when changing equipment, resetting talents, etc.
    private readonly List<StatModifier> _equipmentMods = new List<StatModifier>();
    private readonly List<StatModifier> _talentMods = new List<StatModifier>();
    private readonly List<StatModifier> _runBuffMods = new List<StatModifier>();

    // The cache of final values is recalculated only when modifiers are changed.
    private readonly Dictionary<StatType, float> _cache = new Dictionary<StatType, float>();
    private bool _cacheDirty = true;

    public event Action OnStatsChanged;

    // == Base values ======================================================

    public void SetBaseValue(StatType type, float value)
    {
        _baseValues[type] = value;
        _cacheDirty = true;
    }

    public float GetBaseValue(StatType type) =>
        _baseValues.TryGetValue(type, out float v) ? v : 0f;

    // == Managing modifiers =============================================

    public void AddModifier(StatModifier modifier)
    {
        GetList(modifier.Source).Add(modifier);
        _cacheDirty = true;
    }

    // Replaces all modifiers from the specified source
    public void SetModifiers(ModifierSource source, IEnumerable<StatModifier> modifiers)
    {
        var list = GetList(source);
        list.Clear();
        list.AddRange(modifiers);
        _cacheDirty = true;
        NotifyChanged();
    }

    // Removes all modifiers from the specified source.
    public void ClearModifiers(ModifierSource source)
    {
        GetList(source).Clear();
        _cacheDirty = true;
        NotifyChanged();
    }

    // == Getting the final value =========================================

    /// <summary>
    /// Returns the final value of the stat, taking into account all modifiers.
    /// The result is cached — repeated calls without changes are free.
    /// </summary>
    public float GetFinalValue(StatType type)
    {
        if (_cacheDirty) RebuildCache();
        return _cache.TryGetValue(type, out float v) ? v : GetBaseValue(type);
    }

    // == Internal methods =====================================================

    private void RebuildCache()
    {
        _cache.Clear();

        // Collect all the types that are mentioned at least somewhere.
        var types = new HashSet<StatType>(_baseValues.Keys);
        foreach (var m in _equipmentMods) types.Add(m.Type);
        foreach (var m in _talentMods) types.Add(m.Type);
        foreach (var m in _runBuffMods) types.Add(m.Type);

        foreach (StatType type in types)
        {
            float baseVal = GetBaseValue(type);
            float flatSum = 0f;
            float percentSum = 0f;

            AccumulateMods(_equipmentMods, type, ref flatSum, ref percentSum);
            AccumulateMods(_talentMods, type, ref flatSum, ref percentSum);
            AccumulateMods(_runBuffMods, type, ref flatSum, ref percentSum);

            _cache[type] = (baseVal + flatSum) * (1f + percentSum);
        }

        _cacheDirty = false;
    }

    private static void AccumulateMods(
        List<StatModifier> list,
        StatType type,
        ref float flatSum,
        ref float percentSum)
    {
        foreach (var m in list)
        {
            if (m.Type != type) continue;
            if (m.IsPercent) percentSum += m.Value;
            else flatSum += m.Value;
        }
    }

    private List<StatModifier> GetList(ModifierSource source) => source switch
    {
        ModifierSource.Equipment => _equipmentMods,
        ModifierSource.Talent => _talentMods,
        ModifierSource.RunBuff => _runBuffMods,
        _ => _equipmentMods
    };

    private void NotifyChanged()
    {
        _cacheDirty = true;
        OnStatsChanged?.Invoke();
    }
}
