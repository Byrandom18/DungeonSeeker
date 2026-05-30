using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// The bridge between the shared inventory and this character's StatSystem.
/// Equipment is stored in the party inventory; items reference this member via <see cref="PartyMemberId"/>.
/// </summary>
public class EquipmentComponent : MonoBehaviour
{
    [SerializeField] private string _partyMemberId;
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _characterIcon;

    private static readonly Dictionary<string, Sprite> s_ownerIcons = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, EquipmentComponent> s_ownerComponents =
        new Dictionary<string, EquipmentComponent>();

    private StatSystem _statSystem;
    private InventorySO _inventorySO;
    private bool _inventoryEventsBound;

    public StatSystem GetStatSystem() => _statSystem;

    /// <summary>Stable id used in <see cref="InventoryItemData.EquippedOwnerId"/>.</summary>
    public string PartyMemberId
    {
        get
        {
            if (string.IsNullOrEmpty(_partyMemberId))
                _partyMemberId = $"member_{GetInstanceID()}";
            return _partyMemberId;
        }
    }

    public InventorySO Inventory => _inventorySO;

    public Sprite CharacterIcon => _characterIcon;

    public static Sprite GetIconForOwner(string ownerId)
    {
        if (string.IsNullOrEmpty(ownerId)) return null;
        return s_ownerIcons.TryGetValue(ownerId, out Sprite icon) ? icon : null;
    }

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_displayName))
                return _displayName;

            var stats = GetComponent<PlayerStats>();
            if (stats != null && !string.IsNullOrWhiteSpace(stats.CharacterName))
                return stats.CharacterName;

            return gameObject.name;
        }
    }

    private void Awake()
    {
        _statSystem = new StatSystem();
    }

    private void OnEnable()
    {
        RegisterOwnerIcon();
        TryBindSharedInventory();
        if (RunSystem.Instance != null)
            RunSystem.Instance.OnRunStateChanged += HandleRunStateChanged;
    }

    private void Start()
    {
        TryBindSharedInventory();
    }

    private void OnDisable()
    {
        UnregisterOwnerIcon();
        if (RunSystem.Instance != null)
            RunSystem.Instance.OnRunStateChanged -= HandleRunStateChanged;
        UnbindInventoryEvents();
    }

    private void RegisterOwnerIcon()
    {
        string id = PartyMemberId;
        s_ownerIcons[id] = _characterIcon;
        s_ownerComponents[id] = this;
    }

    private void UnregisterOwnerIcon()
    {
        string id = PartyMemberId;
        if (s_ownerComponents.TryGetValue(id, out EquipmentComponent registered) && registered == this)
        {
            s_ownerIcons.Remove(id);
            s_ownerComponents.Remove(id);
        }
    }

    private void HandleRunStateChanged(bool _)
    {
        UnbindInventoryEvents();
        _inventorySO = null;
        TryBindSharedInventory();
    }

    private void TryBindSharedInventory()
    {
        if (InventoryController.Instance == null)
            return;

        InventorySO shared = InventoryController.Instance.GetInventorySO();
        if (shared == null)
            return;

        if (ReferenceEquals(_inventorySO, shared) && _inventoryEventsBound)
            return;

        UnbindInventoryEvents();
        _inventorySO = shared;
        _inventorySO.OnInventoryChanged += Refresh;
        _inventorySO.RebuildEquippedDictionary();
        Refresh();
        _inventoryEventsBound = true;
    }

    private void UnbindInventoryEvents()
    {
        if (_inventorySO != null && _inventoryEventsBound)
            _inventorySO.OnInventoryChanged -= Refresh;

        _inventoryEventsBound = false;
    }

    public void Refresh()
    {
        if (_inventorySO == null)
            return;

        var mods = BuildEquipmentModifiers();
        _statSystem.SetModifiers(ModifierSource.Equipment, mods);
    }

    public void SetBaseStats(IEnumerable<(StatType type, float value)> baseStats)
    {
        foreach (var (type, value) in baseStats)
            _statSystem.SetBaseValue(type, value);
    }

    private List<StatModifier> BuildEquipmentModifiers()
    {
        var result = new List<StatModifier>();
        if (_inventorySO == null) return result;

        string ownerId = PartyMemberId;
        IReadOnlyList<InventoryItemData> items = _inventorySO.Items;

        foreach (var data in items)
        {
            if (!data.IsEquipped) continue;
            if (data.EquippedOwnerId != ownerId) continue;
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

    private static bool IsPercentStat(StatType type) => type switch
    {
        StatType.AttackMod => true,
        StatType.HealthMod => true,
        StatType.DefenceMod => true,
        _ => false
    };
}
