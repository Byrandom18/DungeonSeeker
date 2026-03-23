using System;
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

    [Header("Equipment only")]
    [SerializeField] private GameObject _equipmentPanel;
    [SerializeField] private TMP_Text _slotText;
    //[SerializeField] private TMP_Text _attackText;
    //[SerializeField] private TMP_Text _defenseText;
    //[SerializeField] private TMP_Text _healthText;
    [SerializeField] private TMP_Text _upgradeLevelText;
    [SerializeField] private Button _equipButton;
    [SerializeField] private TMP_Text _equipButtonText;

    [Header("Resource only")]
    [SerializeField] private GameObject _resourcePanel;
    [SerializeField] private TMP_Text _quantityText;

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
    }

    public void SetDescription(InventoryItemData data, System.Action onEquipClicked)
    {
        ItemSO item = data.Item;

        _itemImage.gameObject.SetActive(item.Sprite != null);
        _itemImage.sprite = item.Sprite;

        _nameText.text = item.Name;
        _descriptionText.text = item.Description;

        int rarityIdx = Mathf.Clamp((int)item.Rarity, 1, RarityColors.Length - 1);
        Color rarityColor = RarityColors[rarityIdx];
        _rarityText.text = RarityLabels[rarityIdx];
        _rarityText.color = rarityColor;
        if (_rarityGradient) _rarityGradient.color = rarityColor;

        SetPanelsActive(item.ItemType, true);

        if (item.ItemType == ItemType.Equipment)
            FillEquipmentPanel(item, data, onEquipClicked);
        else
            FillResourcePanel(data);
    }

    private void FillResourcePanel(InventoryItemData data)
    {
        _quantityText.text = $"x{data.Quantity}";
        _onEquipClicked = null;
    }

    private void SetPanelsActive(ItemType type, bool hasData)
    {
        if (_equipmentPanel) _equipmentPanel.SetActive(hasData && type == ItemType.Equipment);
        if (_resourcePanel) _resourcePanel.SetActive(hasData && type == ItemType.Resource);
    }

    private void FillEquipmentPanel(ItemSO item, InventoryItemData data, System.Action onEquipClicked)
    {
        _slotText.text = item.EquipmentSlot.ToString();

        int lvl = data.UpgradeLevel;
        // mult = 1f + lvl * 0.1f;

        //_attackText.text = item.BaseAttack > 0
        //    ? $"ATK  {Mathf.RoundToInt(item.BaseAttack * mult)}" : "";
        //_defenseText.text = item.BaseDefense > 0
        //    ? $"DEF  {Mathf.RoundToInt(item.BaseDefense * mult)}" : "";
        //_healthText.text = item.BaseHealth > 0
        //    ? $"HP   {Mathf.RoundToInt(item.BaseHealth * mult)}" : "";

        _upgradeLevelText.text = "+" + lvl;

        bool isEquipped = data.IsEquipped;
        _equipButtonText.text = isEquipped ? "Unequip" : "Equip";
        _onEquipClicked = onEquipClicked;
    }
}
