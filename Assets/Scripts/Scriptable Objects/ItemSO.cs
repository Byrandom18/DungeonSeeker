using System.Collections.Generic;
using UnityEngine;

public enum ItemRarity
{
    Common = 1,
    Uncommon = 2,
    Rare = 3,
    Epic = 4,
    Legendary = 5,
    Unique = 6
}

public enum ItemType
{
    Resource,
    Equipment
}

public enum EquipmentSlot
{
    None,
    Helmet,
    Armor,
    Boots,
    Gloves,
    Weapon,
    Ring,
    Amulet
}

[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/ItemSO")]
public class ItemSO : ScriptableObject
{
    public int ID => GetInstanceID();

    [field: SerializeField] public Sprite Sprite { get; private set; }
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField, TextArea] public string Description { get; private set; }

    [field: SerializeField] public ItemType ItemType { get; private set; }
    [field: SerializeField] public ItemRarity Rarity { get; private set; }
    [field: SerializeField] public bool IsStackable { get; private set; }

    [field: SerializeField] public EquipmentSlot EquipmentSlot { get; private set; }
    [field: SerializeField] public int MaxUpgradeLevel { get; private set; } = 6;

    [Header("Stat Pools")]
    [SerializeField] private MainStatPoolSO _mainStatPool;
    public MainStatPoolSO MainStatPool => _mainStatPool;
    [SerializeField] private BonusStatPoolSO _bonusStatPool;
    public BonusStatPoolSO BonusStatPool => _bonusStatPool;

    [Header("Upgrade Settings")]
    [SerializeField] private int _firstBonusAtLevel = 1;
    [SerializeField] private int _bonusEveryNLevels = 1;
    public int FirstBonusAtLevel => _firstBonusAtLevel;
    public int BonusEveryNLevels => _bonusEveryNLevels;

    [SerializeField] private UpgradeRecipeSO _upgradeRecipe;
    public UpgradeRecipeSO UpgradeRecipe => _upgradeRecipe;

    public int ExpectedBonusCountAtLevel(int level)
    {
        if (level < _firstBonusAtLevel || _bonusEveryNLevels <= 0) return 0;
        int count = 1 + (level - _firstBonusAtLevel) / _bonusEveryNLevels;
        return Mathf.Min(count, 6);
    }
}
