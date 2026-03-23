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
}
