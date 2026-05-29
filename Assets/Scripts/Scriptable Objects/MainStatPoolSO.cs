using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MainStatPoolSO", menuName = "Scriptable Objects/MainStatPoolSO")]
public class MainStatPoolSO : ScriptableObject
{
    [SerializeField] private List<MainStatEntry> _entries = new List<MainStatEntry>();
    public IReadOnlyList<MainStatEntry> Entries => _entries;

    public MainStatInstance Roll(ItemRarity rarity)
    {
        if (_entries == null || _entries.Count == 0)
        {
            Debug.LogWarning($"[MainStatPoolSO] {name}: pool empty.");
            return default;
        }

        float totalWeight = 0f;
        foreach (var e in _entries) totalWeight += Mathf.Max(e.Weight, 0f);

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        float multiplier = RarityStatMultiplier.Get(rarity);
        foreach (var e in _entries)
        {
            cumulative += Mathf.Max(e.Weight, 0f);
            if (roll <= cumulative)
            {

                return new MainStatInstance
                {
                    Type = e.Type,
                    BaseValue = e.BaseValue * multiplier,
                    ValueScalePerLevel = e.ValueScalePerLevel
                };
            }
        }

        var first = _entries[0];
        return new MainStatInstance
        {
            Type = first.Type,
            BaseValue = first.BaseValue * multiplier,
            ValueScalePerLevel = first.ValueScalePerLevel
        };
    }
}
