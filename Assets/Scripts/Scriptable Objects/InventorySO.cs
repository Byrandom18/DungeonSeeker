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

        // Списать ресурсы
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

        // Пересчитать основную характеристику
        var newMain = data.MainStat;
        newMain = new MainStatInstance
        {
            Type = newMain.Type,
            BaseValue = newMain.BaseValue,
            ValueScalePerLevel = newMain.ValueScalePerLevel
        };

        // Добавить доп. характеристику если нужно
        var newBonus = new List<BonusStatInstance>(data.BonusStats);
        int expectedBonus = data.Item.ExpectedBonusCountAtLevel(newLevel);
        if (newBonus.Count < expectedBonus && data.Item.BonusStatPool != null)
        {
            if (data.Item.BonusStatPool.TryRoll(newBonus, out var rolled))
                newBonus.Add(rolled);
        }

        _items[itemIndex] = data.WithUpgrade(newLevel, newMain, newBonus);
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

    //private List<float> RecalculateMainStats(ItemSO item, int level)
    //{
    //    var result = new List<float>();
    //    foreach (var def in item.MainStats)
    //        result.Add(def.BaseValue + def.BaseValue * def.ValueScalePerLevel * level);
    //    return result;
    //}

    //private void TryAddBonusStat(ItemSO item, List<BonusStatInstance> current)
    //{
    //    if (item.BonusStatPool.Count == 0) return;

    //    // Исключить уже выпавшие типы
    //    var available = item.BonusStatPool
    //        .Where(b => current.All(c => c.Type != b.Type))
    //        .ToList();

    //    if (available.Count == 0) return;

    //    var chosen = available[UnityEngine.Random.Range(0, available.Count)];
    //    float value = UnityEngine.Random.Range(chosen.MinValue, chosen.MaxValue);
    //    current.Add(new BonusStatInstance(chosen.Type, value));
    //}


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

    // Одна выпавшая основная характеристика
    [SerializeField] private MainStatInstance _mainStat;
    public MainStatInstance MainStat => _mainStat;

    // Выпавшие дополнительные характеристики
    [SerializeField] private List<BonusStatInstance> _bonusStats;
    public IReadOnlyList<BonusStatInstance> BonusStats =>
        _bonusStats ?? (_bonusStats = new List<BonusStatInstance>());

    public InventoryItemData(ItemSO item, int quantity)
    {
        Item = item;
        Quantity = quantity;
        UpgradeLevel = 0;
        IsEquipped = false;
        _bonusStats = new List<BonusStatInstance>();

        // Бросить основную характеристику из пула
        _mainStat = item.MainStatPool != null
            ? item.MainStatPool.Roll()
            : default;
    }

    // Вспомогательные методы создания копии структуры

    public InventoryItemData ChangeQuantity(int newQty) =>
        new InventoryItemData
        {
            Item = Item,
            Quantity = newQty,
            UpgradeLevel = UpgradeLevel,
            IsEquipped = IsEquipped,
            _mainStat = _mainStat,
            _bonusStats = _bonusStats
        };

    public InventoryItemData SetEquipped(bool equipped) =>
        new InventoryItemData
        {
            Item = Item,
            Quantity = Quantity,
            UpgradeLevel = UpgradeLevel,
            IsEquipped = equipped,
            _mainStat = _mainStat,
            _bonusStats = _bonusStats
        };

    public InventoryItemData WithUpgrade(int newLevel, MainStatInstance newMain, List<BonusStatInstance> newBonus) =>
        new InventoryItemData
        {
            Item = Item,
            Quantity = Quantity,
            UpgradeLevel = newLevel,
            IsEquipped = IsEquipped,
            _mainStat = newMain,
            _bonusStats = newBonus
        };
}