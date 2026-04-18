using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// The bridge between the inventory and the character characteristics system.
///
/// Subscribes to InventorySO.OnInventoryChanged,
/// reassembles the StatModifier list of equipped items with each change and transfers
/// them to the StatSystem with a single call to SetModifiers — without going through one at a time.
/// </summary>
public class EquipmentComponent : MonoBehaviour
{
    [SerializeField] private InventorySO _inventorySO;

    private StatSystem _statSystem;

    public StatSystem GetStatSystem() => _statSystem;

    private void Awake()
    {
        _statSystem = new StatSystem();
    }

    private void OnEnable()
    {
        if (_inventorySO == null)
        {
            Debug.LogError($"[EquipmentComponent] InventorySO not assigned to {gameObject.name}");
            return;
        }

        _inventorySO.OnInventoryChanged += Refresh;
        // Restore the equipment dictionary (in case OnEnable is called
        // after loading the scene, when the Dictionary is not filled yet)
        _inventorySO.RebuildEquippedDictionary();
        Refresh();
    }

    private void OnDisable()
    {
        if (_inventorySO != null)
            _inventorySO.OnInventoryChanged -= Refresh;
    }

    private void Start()
    {
        _inventorySO = InventoryController.Instance.GetInventorySO();
    }

    // == public API =========================================================

    /// <summary>
    /// Recalculates all equipment modifiers from the currently equipped items.
    /// It is called automatically when the inventory is changed, but it can be called manually
    /// for example, after loading a save.
    /// </summary>
    public void Refresh()
    {
        var mods = BuildEquipmentModifiers();
        _statSystem.SetModifiers(ModifierSource.Equipment, mods);
    }

    /// <summary>
    /// Sets the basic values of the character's stats (from class, level, etc.).
    /// Called from PlayerStats.InitializeStats() instead of directly assigning fields.
    /// </summary>
    public void SetBaseStats(IEnumerable<(StatType type, float value)> baseStats)
    {
        foreach (var (type, value) in baseStats)
            _statSystem.SetBaseValue(type, value);
    }

    // == Assembling modifiers ==================================================

    private List<StatModifier> BuildEquipmentModifiers()
    {
        var result = new List<StatModifier>();

        if (_inventorySO == null) return result;

        IReadOnlyList<InventoryItemData> items = _inventorySO.Items;

        foreach (var data in items)
        {
            if (!data.IsEquipped) continue;
            if (data.Item == null) continue;
            if (data.Item.ItemType != ItemType.Equipment) continue;

            AddMainStat(result, data);
            AddBonusStats(result, data);
        }

        return result;
    }

    private static void AddMainStat(List<StatModifier> result, InventoryItemData data)
    {
        MainStatInstance main = data.MainStat;

        if (main.BaseValue == 0f && main.ValueScalePerLevel == 0f) return;

        float currentValue = main.GetValue(data.UpgradeLevel);
        // Defining the type of modifier by the name StatType:
        // *Mod = percentage, *Flat = flat
        bool isPercent = IsPercentStat(main.Type);

        result.Add(new StatModifier(main.Type, currentValue, ModifierSource.Equipment, isPercent));
    }

    private static void AddBonusStats(List<StatModifier> result, InventoryItemData data)
    {
        foreach (BonusStatInstance bonus in data.BonusStats)
        {
            bool isPercent = IsPercentStat(bonus.Type);
            result.Add(new StatModifier(bonus.Type, bonus.Value, ModifierSource.Equipment, isPercent));
        }
    }

    /// <summary>
    /// Naming convention from StatTypes.cs:
    /// AttackMod, HealthMod, DefenceMod, SizeMod... — interest rates.
    /// AttackFlat, HealthFlat, DefenceFlat, ManaFlat... — flat ones.
    /// </summary>
    private static bool IsPercentStat(StatType type) => type switch
    {
        StatType.AttackMod => true,
        StatType.HealthMod => true,
        StatType.DefenceMod => true,
        StatType.SizeMod => true,
        StatType.ManaRegenMod => true,
        StatType.SpellDamageMod => true,
        StatType.BaseAttackDamageMod => true,
        StatType.BaseAttackSpeedMod => true,
        StatType.CooldownReduction => true,
        StatType.Luck => true,
        StatType.Resistance => true,
        StatType.CritChance => true,
        StatType.CritDamage => true,
        _ => false
    };
}
