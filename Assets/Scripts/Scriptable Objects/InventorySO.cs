using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "InventorySO", menuName = "Scriptable Objects/InventorySO")]
public class InventorySO : ScriptableObject
{
    [SerializeField] private List<InventoryItemData> _items = new List<InventoryItemData>();

    // Equipped items: slot -> index in _items
    private Dictionary<EquipmentSlot, int> _equippedItems = new Dictionary<EquipmentSlot, int>();

    public event Action OnInventoryChanged;

    public IReadOnlyList<InventoryItemData> Items => _items;

    //initialize
    public void RebuildEquippedDictionary()
    {
        _equippedItems.Clear();
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].IsEquipped && _items[i].Item != null)
            {
                EquipmentSlot slot = _items[i].Item.EquipmentSlot;
                if (!_equippedItems.ContainsKey(slot))
                    _equippedItems[slot] = i;
                else
                {
                    _items[i] = _items[i].SetEquipped(false);
                }
            }
        }
    }

    public bool IsEquipped(int itemIndex) =>
        itemIndex >= 0 && itemIndex < _items.Count && _items[itemIndex].IsEquipped;

    public int? GetEquippedIndex(EquipmentSlot slot) =>
        _equippedItems.TryGetValue(slot, out int idx) ? idx : (int?)null;

    public void AddItem(ItemSO item, int quantity = 1)
    {
        if (item.IsStackable)
        {
            int index = _items.FindIndex(i => i.Item == item);
            if (index >= 0)
            {
                _items[index] = _items[index].ChangeQuantity(_items[index].Quantity + quantity);
                OnInventoryChanged?.Invoke();
                return;
            }
        }

        var newData = new InventoryItemData(item, quantity);
        _items.Add(newData);
        OnInventoryChanged?.Invoke();
    }

    public bool RemoveItem(ItemSO item, int quantity = 1)
    {
        int index = _items.FindIndex(i => i.Item == item);
        if (index < 0) return false;

        int newQuantity = _items[index].Quantity - quantity;
        if (newQuantity <= 0)
        {
            // Unequip if currently equipped
            UnequipIfNeeded(index);
            _items.RemoveAt(index);
        }
        else
        {
            _items[index] = _items[index].ChangeQuantity(newQuantity);
        }

        OnInventoryChanged?.Invoke();
        return true;
    }


    public bool EquipItem(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= _items.Count) return false;

        InventoryItemData data = _items[itemIndex];
        if (data.Item.ItemType != ItemType.Equipment) return false;

        EquipmentSlot slot = data.Item.EquipmentSlot;

        // Unequip whatever is currently in this slot
        if (_equippedItems.TryGetValue(slot, out int prevIndex) && prevIndex != itemIndex)
        {
            if (prevIndex < _items.Count)
                _items[prevIndex] = _items[prevIndex].SetEquipped(false);
        }

        _equippedItems[slot] = itemIndex;
        _items[itemIndex] = _items[itemIndex].SetEquipped(true);

        OnInventoryChanged?.Invoke();
        return true;
    }


    public bool UnequipSlot(EquipmentSlot slot)
    {
        if (!_equippedItems.TryGetValue(slot, out int index)) return false;

        if (index < _items.Count)
            _items[index] = _items[index].SetEquipped(false);

        _equippedItems.Remove(slot);
        OnInventoryChanged?.Invoke();
        return true;
    }


    //upgrade


    public bool CanUpgradeItem(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= _items.Count) return false;

        InventoryItemData data = _items[itemIndex];
        if (data.Item.ItemType != ItemType.Equipment) return false;
        if (data.UpgradeLevel >= data.Item.MaxUpgradeLevel) return false;

        UpgradeRecipeSO recipe = data.Item.UpgradeRecipe;
        if (recipe == null) return true; // free upgrade if no recipe

        return recipe.CanUpgrade(data.UpgradeLevel + 1, _items);
    }

    public bool UpgradeItem(int itemIndex)
    {
        if (!CanUpgradeItem(itemIndex)) return false;

        InventoryItemData data = _items[itemIndex];
        int newLevel = data.UpgradeLevel + 1;

        // —писать ресурсы
        UpgradeRecipeSO recipe = data.Item.UpgradeRecipe;
        if (recipe != null)
        {
            var ingredients = recipe.GetIngredientsForLevel(newLevel);
            if (ingredients != null)
            {
                foreach (var ingredient in ingredients)
                {
                    if (ingredient.Resource == null) continue;
                    ConsumeResource(ingredient.Resource, ingredient.Quantity);
                }
            }
        }

        // ѕересчитать основные характеристики
        var newMainStats = RecalculateMainStats(data.Item, newLevel);

        // ƒобавить доп. характеристику если нужно
        var newBonusStats = new List<BonusStatInstance>(data.BonusStats);
        int expectedBonus = data.Item.ExpectedBonusCountAtLevel(newLevel);
        if (newBonusStats.Count < expectedBonus)
            TryAddBonusStat(data.Item, newBonusStats);

        _items[itemIndex] = data.WithUpgrade(newLevel, newMainStats, newBonusStats);
        OnInventoryChanged?.Invoke();
        return true;
    }

    private void ConsumeResource(ItemSO resource, int quantity)
    {
        int index = _items.FindIndex(i => i.Item == resource);
        if (index < 0) return;

        int newQty = _items[index].Quantity - quantity;
        if (newQty <= 0)
            _items.RemoveAt(index);
        else
            _items[index] = _items[index].ChangeQuantity(newQty);
    }

    private List<float> RecalculateMainStats(ItemSO item, int level)
    {
        var result = new List<float>();
        foreach (var def in item.MainStats)
            result.Add(def.BaseValue + def.BaseValue * def.ValueScalePerLevel * level);
        return result;
    }

    private void TryAddBonusStat(ItemSO item, List<BonusStatInstance> current)
    {
        if (item.BonusStatPool.Count == 0) return;

        // »сключить уже выпавшие типы
        var available = item.BonusStatPool
            .Where(b => current.All(c => c.Type != b.Type))
            .ToList();

        if (available.Count == 0) return;

        var chosen = available[UnityEngine.Random.Range(0, available.Count)];
        float value = UnityEngine.Random.Range(chosen.MinValue, chosen.MaxValue);
        current.Add(new BonusStatInstance(chosen.Type, value));
    }


    // Sorting
    /// <summary>Returns items sorted by Rarity descending.</summary>
    public List<InventoryItemData> GetSortedItems() =>
        _items.OrderByDescending(i => (int)i.Item.Rarity).ToList();

    public List<InventoryItemData> GetResourcesSorted() =>
        _items.Where(i => i.Item.ItemType == ItemType.Resource)
              .OrderByDescending(i => (int)i.Item.Rarity).ToList();

    public List<InventoryItemData> GetEquipmentSorted(EquipmentSlot? filterSlot = null) =>
        _items.Where(i => i.Item.ItemType == ItemType.Equipment &&
                          (filterSlot == null || i.Item.EquipmentSlot == filterSlot))
              .OrderByDescending(i => (int)i.Item.Rarity).ToList();

    private void UnequipIfNeeded(int index)
    {
        foreach (var kv in _equippedItems.ToList())
        {
            if (kv.Value == index)
            {
                _equippedItems.Remove(kv.Key);
                break;
            }
        }
    }
}


[Serializable]
public struct InventoryItemData
{
    public ItemSO Item;
    public int Quantity;
    public int UpgradeLevel;
    public bool IsEquipped;

    // “екущие значени€ основных характеристик (индекс совпадает с Item.MainStats)
    [SerializeField] private List<float> _mainStatValues;
    public IReadOnlyList<float> MainStatValues => _mainStatValues ?? (_mainStatValues = new List<float>());

    // ¬ыпавшие дополнительные характеристики
    [SerializeField] private List<BonusStatInstance> _bonusStats;
    public IReadOnlyList<BonusStatInstance> BonusStats => _bonusStats ?? (_bonusStats = new List<BonusStatInstance>());

    public InventoryItemData(ItemSO item, int quantity)
    {
        Item = item;
        Quantity = quantity;
        UpgradeLevel = 0;
        IsEquipped = false;

        // »нициализировать основные характеристики базовыми значени€ми
        _mainStatValues = new List<float>();
        foreach (var def in item.MainStats)
            _mainStatValues.Add(def.BaseValue);

        _bonusStats = new List<BonusStatInstance>();
    }

    // ¬спомогательные методы создани€ копии структуры

    public InventoryItemData ChangeQuantity(int newQty) =>
        new InventoryItemData
        {
            Item = Item,
            Quantity = newQty,
            UpgradeLevel = UpgradeLevel,
            IsEquipped = IsEquipped,
            _mainStatValues = _mainStatValues,
            _bonusStats = _bonusStats
        };

    public InventoryItemData SetEquipped(bool equipped) =>
        new InventoryItemData
        {
            Item = Item,
            Quantity = Quantity,
            UpgradeLevel = UpgradeLevel,
            IsEquipped = equipped,
            _mainStatValues = _mainStatValues,
            _bonusStats = _bonusStats
        };

    public InventoryItemData ChangeUpgradeLevel(int level) =>
        new InventoryItemData
        {
            Item = Item,
            Quantity = Quantity,
            UpgradeLevel = level,
            IsEquipped = IsEquipped,
            _mainStatValues = _mainStatValues,
            _bonusStats = _bonusStats
        };

    /// <summary>—оздаЄт копию с новым уровнем, пересчитанными основными и новыми доп. характеристиками.</summary>
    public InventoryItemData WithUpgrade(int newLevel, List<float> newMainStats, List<BonusStatInstance> newBonusStats) =>
        new InventoryItemData
        {
            Item = Item,
            Quantity = Quantity,
            UpgradeLevel = newLevel,
            IsEquipped = IsEquipped,
            _mainStatValues = newMainStats,
            _bonusStats = newBonusStats
        };

    /// <summary>¬озвращает текущее значение основной характеристики по типу, или 0 если не найдена.</summary>
    public float GetMainStat(StatType type)
    {
        if (Item == null) return 0f;
        for (int i = 0; i < Item.MainStats.Count; i++)
        {
            if (Item.MainStats[i].Type == type)
                return _mainStatValues != null && i < _mainStatValues.Count
                    ? _mainStatValues[i] : Item.MainStats[i].BaseValue;
        }
        return 0f;
    }
}