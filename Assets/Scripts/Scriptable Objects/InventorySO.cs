using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "InventorySO", menuName = "Scriptable Objects/InventorySO")]
public class InventorySO : ScriptableObject
{
    [SerializeField] private List<InventoryItemData> _items = new List<InventoryItemData>();

    // ownerId -> (slot -> index in _items)
    private readonly Dictionary<string, Dictionary<EquipmentSlot, int>> _equippedByOwner =
        new Dictionary<string, Dictionary<EquipmentSlot, int>>();

    public event Action OnInventoryChanged;
    public event Action OnItemAdded;
    public event Action<int> OnItemRemovedAt;

    public IReadOnlyList<InventoryItemData> Items => _items;

    /// <summary>
    /// Clears all items from this inventory.
    /// </summary>
    public void ClearAll(bool notify = true)
    {
        _items.Clear();
        _equippedByOwner.Clear();
        if (notify)
            NotifyInventoryChanged();
    }

    /// <summary>
    /// Adds prebuilt runtime items directly (used by run/hub transfer).
    /// </summary>
    public void AddItems(IEnumerable<InventoryItemData> items, bool notify = true)
    {
        if (items == null) return;
        foreach (var item in items)
        {
            // Keep equipment instances separate (each has own rolled stats/rarity/upgrade).
            if (item.Item != null && item.Item.IsStackable && item.Item.ItemType == ItemType.Resource)
            {
                int index = _items.FindIndex(i => i.Item == item.Item);
                if (index >= 0)
                {
                    int newQty = _items[index].Quantity + item.Quantity;
                    _items[index] = _items[index].ChangeQuantity(newQty);
                    continue;
                }
            }

            _items.Add(item);
        }
        if (notify)
            NotifyInventoryChanged();
    }

    /// <summary>
    /// Moves all currently equipped items into target inventory.
    /// Source items are removed from this inventory.
    /// </summary>
    public int TransferEquippedTo(InventorySO target)
    {
        if (target == null || target == this) return 0;

        var moved = new List<InventoryItemData>();
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            if (!_items[i].IsEquipped) continue;
            moved.Add(_items[i]);
            _items.RemoveAt(i);
        }

        moved.Reverse();

        if (moved.Count > 0)
            target.AddItems(moved, notify: false);

        NotifyInventoryChanged();
        target.NotifyInventoryChanged();
        return moved.Count;
    }

    /// <summary>
    /// Moves all items to target inventory and empties this inventory.
    /// </summary>
    public int TransferAllTo(InventorySO target)
    {
        if (target == null || target == this) return 0;

        int movedCount = _items.Count;
        if (movedCount == 0) return 0;

        target.AddItems(_items, notify: false);
        _items.Clear();
        _equippedByOwner.Clear();

        NotifyInventoryChanged();
        target.NotifyInventoryChanged();
        return movedCount;
    }

    // ========== initialize ================================================
    public void RebuildEquippedDictionary()
    {
        _equippedByOwner.Clear();
        for (int i = 0; i < _items.Count; i++)
        {
            InventoryItemData data = _items[i];
            if (!data.IsEquipped || data.Item == null)
                continue;

            if (string.IsNullOrEmpty(data.EquippedOwnerId))
            {
                _items[i] = data.SetEquipped(false);
                continue;
            }

            EquipmentSlot slot = data.Item.EquipmentSlot;
            if (!TryRegisterEquippedIndex(data.EquippedOwnerId, slot, i))
                _items[i] = data.SetEquipped(false);
        }
    }

    public bool IsEquipped(int itemIndex) =>
        itemIndex >= 0 && itemIndex < _items.Count && _items[itemIndex].IsEquipped;

    public bool IsEquippedOn(int itemIndex, string ownerId)
    {
        if (!IsEquipped(itemIndex)) return false;
        if (string.IsNullOrEmpty(ownerId)) return false;
        return _items[itemIndex].EquippedOwnerId == ownerId;
    }

    public int? GetEquippedIndex(EquipmentSlot slot, string ownerId)
    {
        if (string.IsNullOrEmpty(ownerId)) return null;
        if (!_equippedByOwner.TryGetValue(ownerId, out var slots)) return null;
        return slots.TryGetValue(slot, out int idx) ? idx : (int?)null;
    }


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

    public bool EquipItem(int itemIndex, string ownerId)
    {
        if (string.IsNullOrEmpty(ownerId)) return false;
        if (itemIndex < 0 || itemIndex >= _items.Count) return false;

        InventoryItemData data = _items[itemIndex];
        if (data.Item == null || data.Item.ItemType != ItemType.Equipment) return false;

        EquipmentSlot slot = data.Item.EquipmentSlot;

        if (GetEquippedIndex(slot, ownerId) is int prevIndex && prevIndex != itemIndex)
            UnequipItemAt(prevIndex);

        if (data.IsEquipped && data.EquippedOwnerId != ownerId)
            UnequipItemAt(itemIndex);

        _items[itemIndex] = data.SetEquipped(true, ownerId);
        TryRegisterEquippedIndex(ownerId, slot, itemIndex);

        NotifyInventoryChanged();
        return true;
    }

    public bool UnequipSlot(EquipmentSlot slot, string ownerId)
    {
        if (string.IsNullOrEmpty(ownerId)) return false;
        if (GetEquippedIndex(slot, ownerId) is not int index) return false;
        if (!UnequipItemAt(index)) return false;

        NotifyInventoryChanged();
        return true;
    }

    /// <summary>
    /// Removes a single item entry and updates equipped indices.
    /// </summary>
    public bool RemoveItemAt(int index, bool notify = true)
    {
        if (index < 0 || index >= _items.Count) return false;

        UnequipIfNeeded(index);
        _items.RemoveAt(index);
        OnItemRemovedAt?.Invoke(index);

        if (notify)
            NotifyInventoryChanged();
        else
            RebuildEquippedDictionary();

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
            Debug.Log($"[InventorySO] ��������� �������� ������: {_items.Count(item => item.Item?.ItemType == ItemType.Equipment)} ���������");
        }
    }

    private void NotifyInventoryChanged()
    {
        RebuildEquippedDictionary();
        OnInventoryChanged?.Invoke();
    }

    private bool UnequipItemAt(int index)
    {
        if (index < 0 || index >= _items.Count) return false;

        InventoryItemData data = _items[index];
        if (!data.IsEquipped) return false;

        RemoveEquippedRegistration(data.EquippedOwnerId, data.Item?.EquipmentSlot ?? default, index);
        _items[index] = data.SetEquipped(false);
        return true;
    }

    private void UnequipIfNeeded(int index)
    {
        if (index < 0 || index >= _items.Count) return;
        InventoryItemData data = _items[index];
        if (!data.IsEquipped) return;

        RemoveEquippedRegistration(data.EquippedOwnerId, data.Item?.EquipmentSlot ?? default, index);
    }

    private bool TryRegisterEquippedIndex(string ownerId, EquipmentSlot slot, int index)
    {
        if (!_equippedByOwner.TryGetValue(ownerId, out var slots))
        {
            slots = new Dictionary<EquipmentSlot, int>();
            _equippedByOwner[ownerId] = slots;
        }

        if (slots.TryGetValue(slot, out int existing) && existing != index)
            return false;

        slots[slot] = index;
        return true;
    }

    private void RemoveEquippedRegistration(string ownerId, EquipmentSlot slot, int index)
    {
        if (string.IsNullOrEmpty(ownerId)) return;
        if (!_equippedByOwner.TryGetValue(ownerId, out var slots)) return;
        if (slots.TryGetValue(slot, out int registered) && registered == index)
            slots.Remove(slot);
        if (slots.Count == 0)
            _equippedByOwner.Remove(ownerId);
    }
}


[Serializable]
public struct InventoryItemData
{
    public ItemSO Item;
    public int Quantity;
    public int UpgradeLevel;
    public bool IsEquipped;
    /// <summary>Party member id from <see cref="EquipmentComponent.PartyMemberId"/>.</summary>
    public string EquippedOwnerId;
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
        EquippedOwnerId = null;
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
            EquippedOwnerId = EquippedOwnerId,
            Rarity = Rarity,
            MaxUpgradeLevel = MaxUpgradeLevel,
            _mainStat = _mainStat,
            _bonusStats = _bonusStats
        };

    public InventoryItemData SetEquipped(bool equipped, string ownerId = null) =>
        new InventoryItemData
        {
            Item = Item,
            Quantity = Quantity,
            UpgradeLevel = UpgradeLevel,
            IsEquipped = equipped,
            EquippedOwnerId = equipped ? ownerId : null,
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
            EquippedOwnerId = EquippedOwnerId,
            Rarity = Rarity,
            MaxUpgradeLevel = MaxUpgradeLevel,
            _mainStat = newMain,
            _bonusStats = newBonus
        };
}