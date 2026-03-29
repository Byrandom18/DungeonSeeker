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

    public void RebuildEquippedDictionary()
    {
        _equippedItems.Clear();
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].IsEquipped && _items[i].Item != null)
            {
                EquipmentSlot slot = _items[i].Item.EquipmentSlot;
                // Если в одном слоте несколько помеченных — оставляем первый
                if (!_equippedItems.ContainsKey(slot))
                    _equippedItems[slot] = i;
                else
                {
                    // Снять лишний флаг
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

        _items.Add(new InventoryItemData(item, quantity));
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

    public bool UpgradeItem(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= _items.Count) return false;

        InventoryItemData data = _items[itemIndex];
        if (data.Item.ItemType != ItemType.Equipment) return false;
        if (data.UpgradeLevel >= data.Item.MaxUpgradeLevel) return false;

        _items[itemIndex] = data.ChangeUpgradeLevel(data.UpgradeLevel + 1);
        OnInventoryChanged?.Invoke();
        return true;
    }

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

    public InventoryItemData(ItemSO item, int quantity)
    {
        Item = item;
        Quantity = quantity;
        UpgradeLevel = 0;
        IsEquipped = false;
    }

    public InventoryItemData ChangeQuantity(int newQty) =>
        new InventoryItemData { Item = Item, Quantity = newQty, UpgradeLevel = UpgradeLevel, IsEquipped = IsEquipped };

    public InventoryItemData ChangeUpgradeLevel(int level) =>
        new InventoryItemData { Item = Item, Quantity = Quantity, UpgradeLevel = level, IsEquipped = IsEquipped };

    public InventoryItemData SetEquipped(bool equipped) =>
        new InventoryItemData { Item = Item, Quantity = Quantity, UpgradeLevel = UpgradeLevel, IsEquipped = equipped };
}