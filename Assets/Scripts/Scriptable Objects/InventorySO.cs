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
    public event Action OnItemAdded;
    public event Action<int> OnItemRemovedAt;

    public IReadOnlyList<InventoryItemData> Items => _items;

    // ========== initialize ================================================
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


    //======== Drop System ====================================================================
    public void AddDroppedItem(InventoryItemData droppedData) // Using:  _dropTable.RollMultipleDrops(playerLuck, droppedData => { _inventorySO.AddDroppedItem(droppedData); } );
    {
        if (droppedData.Item.IsStackable && droppedData.Item.ItemType == ItemType.Resource)
        {
            int index = _items.FindIndex(i => i.Item == droppedData.Item);
            if (index >= 0)
            {
                int newQty = _items[index].Quantity + droppedData.Quantity;
                _items[index] = _items[index].ChangeQuantity(newQty);
                NotifyInventoryChanged();
                return;
            }
        }

        _items.Add(droppedData);
        NotifyInventoryChanged();
        OnItemAdded?.Invoke();
    }

    //======== Manual adding (shop/quest...) =====================================================

    public void AddItem(ItemSO item, int quantity = 1)
    {
        if (item.IsStackable)
        {
            int index = _items.FindIndex(i => i.Item == item);
            if (index >= 0)
            {
                _items[index] = _items[index].ChangeQuantity(_items[index].Quantity + quantity);
                NotifyInventoryChanged();
                return;
            }
        }

        var newData = new InventoryItemData(item, quantity);
        _items.Add(newData);
        NotifyInventoryChanged();
        OnItemAdded?.Invoke();
    }

    public bool RemoveItem(ItemSO item, int quantity = 1)
    {
        int index = _items.FindIndex(i => i.Item == item);
        if (index < 0) return false;

        int newQuantity = _items[index].Quantity - quantity;
        if (newQuantity <= 0)
        {
            UnequipIfNeeded(index);
            _items.RemoveAt(index);
            OnItemRemovedAt?.Invoke(index);
        }
        else
        {
            _items[index] = _items[index].ChangeQuantity(newQuantity);
        }

        NotifyInventoryChanged();
        return true;
    }

    //=========== Equip ====================================================================

    public bool EquipItem(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= _items.Count) return false;

        InventoryItemData data = _items[itemIndex];
        if (data.Item.ItemType != ItemType.Equipment) return false;

        EquipmentSlot slot = data.Item.EquipmentSlot;

        if (_equippedItems.TryGetValue(slot, out int prevIndex) && prevIndex != itemIndex)
        {
            if (prevIndex < _items.Count)
                _items[prevIndex] = _items[prevIndex].SetEquipped(false);
        }

        _equippedItems[slot] = itemIndex;
        _items[itemIndex] = _items[itemIndex].SetEquipped(true);

        NotifyInventoryChanged();
        return true;
    }


    public bool UnequipSlot(EquipmentSlot slot)
    {
        if (!_equippedItems.TryGetValue(slot, out int index)) return false;

        if (index < _items.Count)
            _items[index] = _items[index].SetEquipped(false);

        _equippedItems.Remove(slot);
        NotifyInventoryChanged();
        return true;
    }


    //========== Upgrade ======================================================================


    public bool CanUpgradeItem(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= _items.Count) return false;

        InventoryItemData data = _items[itemIndex];
        if (data.Item.ItemType != ItemType.Equipment) return false;
        if (data.UpgradeLevel >= data.MaxUpgradeLevel) return false;

        UpgradeRecipeSO recipe = data.Item.UpgradeRecipe;
        if (recipe == null)
        {
            Debug.LogError($"{data.Item.name} has no recipe");
            return false;
        }

        return recipe.CanUpgrade(data.UpgradeLevel + 1, _items);
    }

    public bool UpgradeItem(int itemIndex)
    {
        if (!CanUpgradeItem(itemIndex)) return false;

        InventoryItemData data = _items[itemIndex];
        int newLevel = data.UpgradeLevel + 1;

        var newMain = data.MainStat;
        newMain = new MainStatInstance
        {
            Type = newMain.Type,
            BaseValue = newMain.BaseValue,
            ValueScalePerLevel = newMain.ValueScalePerLevel
        };

        var newBonus = new List<BonusStatInstance>(data.BonusStats);
        int expectedBonus = data.Item.ExpectedBonusCountAtLevel(newLevel);
        if (newBonus.Count < expectedBonus && data.Item.BonusStatPool != null)
        {
            if (data.Item.BonusStatPool.TryRoll(newBonus, out var rolled))
                newBonus.Add(rolled);
        }

        _items[itemIndex] = data.WithUpgrade(newLevel, newMain, newBonus);

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

        NotifyInventoryChanged();
        return true;
    }




    // ============ Sorting ==============================================================
    // Returns items sorted by Rarity descending
    public List<InventoryItemData> GetSortedItems() =>
        _items.OrderByDescending(i => (int)i.Item.Rarity).ToList();

    public List<InventoryItemData> GetResourcesSorted() =>
        _items.Where(i => i.Item.ItemType == ItemType.Resource)
              .OrderByDescending(i => (int)i.Rarity).ToList();

    public List<InventoryItemData> GetEquipmentSorted(EquipmentSlot? filterSlot = null) =>
        _items.Where(i => i.Item.ItemType == ItemType.Equipment &&
                          (filterSlot == null || i.Item.EquipmentSlot == filterSlot))
              .OrderByDescending(i => (int)i.Rarity).ToList();

    // ========= Mix ======================================================================

    private void ConsumeResource(ItemSO resource, int quantity)
    {
        int index = _items.FindIndex(i => i.Item == resource);
        if (index < 0) return;

        int newQty = _items[index].Quantity - quantity;
        if (newQty <= 0)
        {
            _items.RemoveAt(index);
            OnItemRemovedAt?.Invoke(index);
        }
        else
        {
            _items[index] = _items[index].ChangeQuantity(newQty);
        }
    }

    /// <summary>
    /// Updates ALL bonus stats for ALL items of equipment in the current pool. Call BonusStatPoolSO after the change.
    /// </summary>
    public void RefreshAllBonusStats()
    {
        bool anyChanged = false;

        for (int i = 0; i < _items.Count; i++)
        {
            var data = _items[i];
            if (data.Item?.ItemType != ItemType.Equipment) continue;
            if (data.Item.BonusStatPool == null) continue;

            var newBonusList = new List<BonusStatInstance>();

            foreach (var bonus in data.BonusStats)
            {
                if (data.Item.BonusStatPool.TryGetRandomValueForType(bonus.Type, out float newValue))
                {
                    newBonusList.Add(new BonusStatInstance(bonus.Type, newValue));
                }
                else
                {
                    newBonusList.Add(bonus); 
                }
            }

            _items[i] = data.WithUpgrade(data.UpgradeLevel, data.MainStat, newBonusList);
            anyChanged = true;
        }

        if (anyChanged)
        {
            NotifyInventoryChanged();
            Debug.Log($"[InventorySO] Обновлено бонусных статов: {_items.Count(item => item.Item?.ItemType == ItemType.Equipment)} предметов");
        }
    }

    private void NotifyInventoryChanged()
    {
        RebuildEquippedDictionary();
        OnInventoryChanged?.Invoke();
    }

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
    public ItemRarity Rarity;
    public int MaxUpgradeLevel;

    [SerializeField] private MainStatInstance _mainStat;
    public MainStatInstance MainStat => _mainStat;

    [SerializeField] private List<BonusStatInstance> _bonusStats;
    public IReadOnlyList<BonusStatInstance> BonusStats =>
        _bonusStats ?? (_bonusStats = new List<BonusStatInstance>());

    public InventoryItemData(ItemSO item, int quantity, ItemRarity rarity = ItemRarity.Common, int maxUpgradeLevel = 1)
    {
        Item = item;
        Quantity = quantity;
        UpgradeLevel = 0;
        IsEquipped = false;
        Rarity = rarity;
        MaxUpgradeLevel = maxUpgradeLevel;

        _bonusStats = new List<BonusStatInstance>();

        _mainStat = item.MainStatPool != null
            ? item.MainStatPool.Roll()
            : default;
    }


    public InventoryItemData ChangeQuantity(int newQty) =>
        new InventoryItemData
        {
            Item = Item,
            Quantity = newQty,
            UpgradeLevel = UpgradeLevel,
            IsEquipped = IsEquipped,
            Rarity = Rarity,
            MaxUpgradeLevel = MaxUpgradeLevel,
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
            Rarity = Rarity,
            MaxUpgradeLevel = MaxUpgradeLevel,
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
            Rarity = Rarity,
            MaxUpgradeLevel = MaxUpgradeLevel,
            _mainStat = newMain,
            _bonusStats = newBonus
        };
}