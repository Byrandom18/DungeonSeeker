using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DropTableSO", menuName = "Scriptable Objects/DropTableSO")]
public class DropTableSO : ScriptableObject
{

    [Serializable]
    public struct DropEntry
    {
        public ItemSO Item;
        public float Weight;
        [Header("Resorces")]
        public int MinQuantity;
        public int MaxQuantity;
    }

    [SerializeField] private List<DropEntry> _entries = new List<DropEntry>();

    public void RollDrops(float playerLuck, Action<InventoryItemData> onEachDrop)
    {
        if (_entries.Count == 0) return;

        float totalWeight = 0f;
        foreach (var e in _entries) totalWeight += e.Weight;

        if (totalWeight <= 0) return;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in _entries)
        {
            cumulative += entry.Weight;
            if (roll <= cumulative)
            {
                ItemRarity rarity = GameUtils.Utils.RollRarity(playerLuck);
                int maxLevel = (int)rarity;

                int qty = entry.Item.ItemType == ItemType.Resource
                    ? UnityEngine.Random.Range(entry.MinQuantity, entry.MaxQuantity + 1)
                    : 1;

                // Удача немного увеличивает количество ресурсов
                if (entry.Item.ItemType == ItemType.Resource)
                    qty = Mathf.Max(1, Mathf.RoundToInt(qty * (1f + playerLuck / 60f)));

                var data = new InventoryItemData(entry.Item, qty, rarity, maxLevel);
                onEachDrop?.Invoke(data);
                return; // один предмет за один ролл (можно убрать return для нескольких)
            }
        }
    }
}