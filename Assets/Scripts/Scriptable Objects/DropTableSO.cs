using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DropTableSO", menuName = "Scriptable Objects/DropTableSO")]
public class DropTableSO : ScriptableObject
{
    //[Header("MultiDrop")]
    //[SerializeField, Min(0)] private int _minTotalDrops = 1;
    //[SerializeField, Min(0)] private int _maxTotalDrops = 3;
    //[SerializeField, Range(0f, 1f)] private float _nothingChancePerRoll = 0.1f;
    //[Tooltip("Chance to drop resource instead equipment (example: 0.8 = 80 res, 20 equip)")]
    //[SerializeField, Range(0f, 1f)] private float _resorceChance = 0.8f;

    [Header("Resources (fixed rarity)")]
    [SerializeField] private List<ResourceDropEntry> _resourceDrops = new List<ResourceDropEntry>();

    [Header("Equipment (flexible rarity)")]
    [SerializeField] private List<EquipmentDropEntry> _equipmentDrops = new List<EquipmentDropEntry>();

    [Header("Always Drop")]
    [SerializeField] private List<AlwaysDropEntry> _alwaysDrops = new List<AlwaysDropEntry>();

    [Serializable]
    public struct ResourceDropEntry
    {
        public ItemSO Resource;
        [Range(0f, 100f)] public float Weight;
        [Min(1)] public int MinQuantity;
        [Min(1)] public int MaxQuantity;

        [Header("Override quantity by rolled rarity (optional)")]
        public bool UseRarityQuantity;
        public List<RarityQuantityOverride> RarityQuantities;
    }

    [Serializable]
    public struct RarityQuantityOverride
    {
        public ItemRarity Rarity;
        [Min(1)] public int MinQuantity;
        [Min(1)] public int MaxQuantity;
    }

    [Serializable]
    public struct EquipmentDropEntry
    {
        public ItemSO Equipment;
        [Range(0f, 100f)] public float Weight;
    }

    [Serializable]
    public struct AlwaysDropEntry
    {
        public ItemSO Item;
        [Min(1)] public int MinQuantity;
        [Min(1)] public int MaxQuantity;
    }

    public void RollMultipleDrops(float totalRarity, int totalDropsCount, float chancePerDropRoll, float resourceChance, Action<InventoryItemData> onEachDrop)
    {
        if (_resourceDrops.Count == 0 && _equipmentDrops.Count == 0 && _alwaysDrops.Count == 0) return;

        // always drop
        foreach (var always in _alwaysDrops)
        {
            if (always.Item == null) continue;

            int quantity = UnityEngine.Random.Range(always.MinQuantity, always.MaxQuantity + 1);
            quantity = Mathf.Max(1, quantity);

            var data = new InventoryItemData(always.Item, quantity, always.Item.Rarity, (int)always.Item.Rarity);
            onEachDrop?.Invoke(data);
        }

        // usual drops
        for (int i = 0; i < totalDropsCount; i++)
        {
            if (UnityEngine.Random.value > chancePerDropRoll) continue;

            if (UnityEngine.Random.value < resourceChance && _resourceDrops.Count > 0)
                TryRollResource(totalRarity, onEachDrop);
            else if (_equipmentDrops.Count > 0)
                TryRollEquipment(totalRarity, onEachDrop);
        }
    }

    private void TryRollResource(float totalRarity, Action<InventoryItemData> onEachDrop)
    {
        ItemRarity rarity = GameUtils.Utils.RollRarity(totalRarity);
        var candidates = new List<ResourceDropEntry>();
        float totalWeight = 0f;

        foreach (var entry in _resourceDrops)
        {
            if (entry.Resource == null) continue;

            // A regular resource with matching rarity OR a resource with userityquantity
            bool matchesRarity = entry.Resource.Rarity == rarity && !entry.UseRarityQuantity;
            bool isRarityQuantity = entry.UseRarityQuantity;

            if (matchesRarity || isRarityQuantity)
            {
                candidates.Add(entry);
                totalWeight += entry.Weight;
            }
        }

        if (totalWeight <= 0) return;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in candidates)
        {
            cumulative += entry.Weight;
            if (roll <= cumulative)
            {
                int quantity;

                if (entry.UseRarityQuantity && entry.RarityQuantities != null)
                {
                    var rarityOverride = entry.RarityQuantities.Find(r => r.Rarity == rarity);

                    // If there is no record for a rare item, we take the nearest smaller one.
                    if (rarityOverride.MinQuantity == 0)
                        rarityOverride = GetFallbackRarityOverride(entry.RarityQuantities, rarity);

                    quantity = UnityEngine.Random.Range(rarityOverride.MinQuantity, rarityOverride.MaxQuantity + 1);
                }
                else
                {
                    quantity = UnityEngine.Random.Range(entry.MinQuantity, entry.MaxQuantity + 1);
                }

                quantity = Mathf.Max(1, quantity);

                // Редкость дропа всегда из ItemSO
                var data = new InventoryItemData(entry.Resource, quantity, entry.Resource.Rarity, (int)entry.Resource.Rarity);
                onEachDrop?.Invoke(data);
                return;
            }
        }
    }

    private RarityQuantityOverride GetFallbackRarityOverride(List<RarityQuantityOverride> overrides, ItemRarity target)
    {
        // nearest rarity below the target
        RarityQuantityOverride best = overrides[0];
        foreach (var o in overrides)
        {
            if (o.Rarity <= target && o.Rarity >= best.Rarity)
                best = o;
        }
        return best;
    }

    private void TryRollEquipment(float totalRarity, Action<InventoryItemData> onEachDrop)
    {
        ItemRarity rarity = GameUtils.Utils.RollRarity(totalRarity);
        int maxLevel = (int)rarity;

        float totalWeight = 0f;
        foreach (var e in _equipmentDrops) totalWeight += e.Weight;
        if (_equipmentDrops.Count == 0 || totalWeight <= 0) return;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in _equipmentDrops)
        {
            cumulative += entry.Weight;
            if (roll <= cumulative)
            {
                var data = new InventoryItemData(entry.Equipment, 1, rarity, maxLevel);
                onEachDrop?.Invoke(data);
                return;
            }
        }
    }
}