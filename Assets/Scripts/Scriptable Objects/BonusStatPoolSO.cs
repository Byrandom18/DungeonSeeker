using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "BonusStatPoolSO", menuName = "Scriptable Objects/BonusStatPoolSO")]
public class BonusStatPoolSO : ScriptableObject
{
    [SerializeField] private List<StatBonusPoolEntry> _pools = new List<StatBonusPoolEntry>();
    public IReadOnlyList<StatBonusPoolEntry> Pools => _pools;

    public bool TryRoll(IReadOnlyList<BonusStatInstance> existing, out BonusStatInstance result)
    {
        result = default;
        if (_pools == null || _pools.Count == 0) return false;

        var availablePools = _pools
            .Where(p => p.Options != null && p.Options.Count > 0 &&
                        existing.All(b => b.Type != p.Type))
            .ToList();

        if (availablePools.Count == 0) return false;

        float totalTypeWeight = 0f;
        foreach (var p in availablePools)
            totalTypeWeight += Mathf.Max(p.TypeWeight, 0f);

        if (totalTypeWeight <= 0f) return false;

        float typeRoll = Random.Range(0f, totalTypeWeight);
        float cumulative = 0f;

        StatBonusPoolEntry selectedPool = availablePools[0];

        foreach (var pool in availablePools)
        {
            cumulative += Mathf.Max(pool.TypeWeight, 0f);
            if (typeRoll <= cumulative)
            {
                selectedPool = pool;
                break;
            }
        }

        float totalOptionWeight = 0f;
        foreach (var opt in selectedPool.Options)
            totalOptionWeight += Mathf.Max(opt.Weight, 0f);

        if (totalOptionWeight <= 0f) return false;

        float valueRoll = Random.Range(0f, totalOptionWeight);
        cumulative = 0f;

        foreach (var opt in selectedPool.Options)
        {
            cumulative += Mathf.Max(opt.Weight, 0f);
            if (valueRoll <= cumulative)
            {
                result = new BonusStatInstance(selectedPool.Type, opt.Value);
                return true;
            }
        }

        // fallback
        result = new BonusStatInstance(selectedPool.Type, selectedPool.Options[0].Value);
        return true;
    }

    public bool TryGetRandomValueForType(StatType type, out float value)
    {
        value = 0f;

        var pool = _pools.FirstOrDefault(p => p.Type == type);
        if (pool.Options == null || pool.Options.Count == 0) return false;

        float totalWeight = 0f;
        foreach (var opt in pool.Options)
            totalWeight += Mathf.Max(opt.Weight, 0f);

        if (totalWeight <= 0f) return false;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var opt in pool.Options)
        {
            cumulative += Mathf.Max(opt.Weight, 0f);
            if (roll <= cumulative)
            {
                value = opt.Value;
                return true;
            }
        }

        value = pool.Options[0].Value;
        return true;
    }
}