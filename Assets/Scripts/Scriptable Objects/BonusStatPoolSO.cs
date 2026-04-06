using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "BonusStatPoolSO", menuName = "Scriptable Objects/BonusStatPoolSO")]
public class BonusStatPoolSO : ScriptableObject
{
    [SerializeField] private List<BonusStatEntry> _entries = new List<BonusStatEntry>();
    public IReadOnlyList<BonusStatEntry> Entries => _entries;

    /// <summary>
    /// Случайно выбирает одну характеристику из пула (с учётом весов),
    /// исключая уже выпавшие типы. Возвращает false если добавить нечего.
    /// </summary>
    public bool TryRoll(IReadOnlyList<BonusStatInstance> existing, out BonusStatInstance result)
    {
        result = default;
        if (_entries == null || _entries.Count == 0) return false;

        var available = _entries
            .Where(e => existing.All(b => b.Type != e.Type))
            .ToList();

        if (available.Count == 0) return false;

        float totalWeight = 0f;
        foreach (var e in available) totalWeight += Mathf.Max(e.Weight, 0f);

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var e in available)
        {
            cumulative += Mathf.Max(e.Weight, 0f);
            if (roll <= cumulative)
            {
                result = new BonusStatInstance(e.Type, Random.Range(e.MinValue, e.MaxValue));
                return true;
            }
        }

        // Fallback
        var first = available[0];
        result = new BonusStatInstance(first.Type, Random.Range(first.MinValue, first.MaxValue));
        return true;
    }
}