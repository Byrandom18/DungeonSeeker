using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryDescription : MonoBehaviour
{
    [Header("Common")]
    [SerializeField] private Image _itemImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _rarityText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private Image _rarityGradient;           // decorative bar colored by rarity

    [Header("Equipment")]
    [SerializeField] private GameObject _equipmentPanel;
    [SerializeField] private TMP_Text _slotText;
    [SerializeField] private TMP_Text _upgradeLevelText;
    [SerializeField] private TMP_Text _mainStatsText;
    [SerializeField] private TMP_Text _bonusStatsText;
    [SerializeField] private GameObject _bonusStatsHeader;

    [SerializeField] private Button _equipButton;
    [SerializeField] private TMP_Text _equipButtonText;


    private static readonly Color[] RarityColors =
    {
        Color.clear,                                  // 0 - unused
        new Color(0.71f, 0.71f, 0.71f),               // 1 - Common    (grey)
        new Color(0.40f, 0.80f, 0.40f),               // 2 - Uncommon  (green)
        new Color(0.28f, 0.55f, 0.90f),               // 3 - Rare      (blue)
        new Color(0.60f, 0.25f, 0.85f),               // 4 - Epic      (purple)
        new Color(0.98f, 0.80f, 0.10f),               // 5 - Legendary (gold)
        new Color(1f, 0.25f, 0f)                      // 6 - Unique    (orange)
    };

    private static readonly string[] RarityLabels =
       { "", "Common", "Uncommon", "Rare", "Epic", "Legendary", "Unique" };

    private System.Action _onEquipClicked;

    private void Awake()
    {
        _equipButton?.onClick.AddListener(() => _onEquipClicked?.Invoke());
        ResetDescription();
    }

    public void ResetDescription()
    {
        _itemImage.gameObject.SetActive(false);
        _nameText.text = "";
        _rarityText.text = "";
        _descriptionText.text = "";
        if (_rarityGradient) _rarityGradient.color = Color.clear;
        SetPanelsActive(ItemType.Resource, false);
        _onEquipClicked = null;
    }

    public void SetDescription(InventoryItemData data, System.Action onEquipClicked, bool isEquippedOnSelectedCharacter = false)
    {
        ItemSO item = data.Item;

        _itemImage.gameObject.SetActive(item.Sprite != null);
        _itemImage.sprite = item.Sprite;

        _nameText.text = item.Name;
        _descriptionText.text = item.Description;

        int rarityIdx = Mathf.Clamp((int)data.Rarity, 1, RarityColors.Length - 1);
        Color rarityColor = RarityColors[rarityIdx];
        _rarityText.text = RarityLabels[rarityIdx];
        _rarityText.color = rarityColor;
        if (_rarityGradient) _rarityGradient.color = rarityColor;

        SetPanelsActive(item.ItemType, true);

        if (item.ItemType == ItemType.Equipment)
            FillEquipmentPanel(item, data, onEquipClicked, isEquippedOnSelectedCharacter);
        else
            FillResourcePanel(data);
    }

    private void FillResourcePanel(InventoryItemData data)
    {
        if (_equipmentPanel) _equipmentPanel.SetActive(false);
        if (_upgradeLevelText)
        {
            _upgradeLevelText.gameObject.SetActive(true);
            _upgradeLevelText.text = $"x{data.Quantity}";
        }
        if (_slotText) _slotText.text = ItemType.Resource.ToString();
        _onEquipClicked = null;
    }

    private void SetPanelsActive(ItemType type, bool hasData)
    {
        if (_equipmentPanel) _equipmentPanel.SetActive(hasData && type == ItemType.Equipment);
        if (_upgradeLevelText) _upgradeLevelText.gameObject.SetActive(hasData);
    }

    private void FillEquipmentPanel(ItemSO item, InventoryItemData data, System.Action onEquipClicked, bool isEquippedOnSelectedCharacter)
    {
        if (_equipmentPanel) _equipmentPanel.SetActive(true);
        if (_slotText) _slotText.text = item.EquipmentSlot.ToString();
        if (_upgradeLevelText) _upgradeLevelText.text = $"+{data.UpgradeLevel}";
        // main stat
        if (_mainStatsText != null)
        {
            MainStatInstance main = data.MainStat;
            float currentValue = main.GetValue(data.UpgradeLevel);
            _mainStatsText.text = $"{StatLabel(main.Type)}  {FormatValue(currentValue)}";
        }

        // additional stat
        bool hasBonus = data.BonusStats.Count > 0;
        if (_bonusStatsHeader) _bonusStatsHeader.SetActive(hasBonus);
        if (_bonusStatsText != null)
        {
            _bonusStatsText.gameObject.SetActive(hasBonus);
            if (hasBonus)
            {
                var sb = new StringBuilder();
                foreach (var bonus in data.BonusStats)
                    sb.AppendLine($"{StatLabel(bonus.Type)}  +{FormatValue(bonus.Value)}");
                _bonusStatsText.text = sb.ToString().TrimEnd();
            }
        }

        if (_equipButtonText) _equipButtonText.text = isEquippedOnSelectedCharacter ? "Unequip" : "Equip";
        _onEquipClicked = onEquipClicked;
    }

    private static string StatLabel(StatType type) => type switch
    {
        StatType.AttackMod           => "ATK%",
        StatType.AttackFlat          => "ATK",
        StatType.HealthMod           => "HP%",
        StatType.HealthFlat          => "HP",
        StatType.DefenceMod          => "DEF%",
        StatType.DefenceFlat         => "DEF",
        StatType.Resistance          => "Resist",
        StatType.CritChance          => "Crit. Rate",
        StatType.CritDamage          => "Crit. DMG",
        StatType.SizeMod             => "AOE",
        StatType.ManaFlat            => "Mana",
        StatType.ManaRegenMod        => "Mana Regen.",
        StatType.SpellDamageMod      => "Spell DMG",
        StatType.BaseAttackDamageMod => "Attack DMG",
        StatType.BaseAttackSpeedMod  => "Attack Speed",
        StatType.CooldownReduction   => "CD Red.",
        StatType.Luck                => "Luck",
            _ => type.ToString()
    };

    private static string FormatValue(float value) =>
        value == Mathf.Floor(value)
            ? ((int)value).ToString()
            : value.ToString("F1"); //round to 1 decimal
}
