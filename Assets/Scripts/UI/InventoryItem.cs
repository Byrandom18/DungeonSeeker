using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image _itemImage;
    [SerializeField] private TMP_Text _quantityText;
    [SerializeField] private Image _rarityGradientImage;
    [SerializeField] private Image _selectionBorderImage;
    [SerializeField] private GameObject _equippedBadge;   

    public InventoryItemData ItemData { get; private set; }
    public int InventoryIndex { get; private set; }

    public event Action<InventoryItem> OnItemClicked;
    public event Action<InventoryItem> OnItemHovered;
    public event Action<InventoryItem> OnItemUnhovered;

    private static readonly Color[] RarityColors = new Color[]
    {
        Color.clear,                                  // 0 - unused
        new Color(0.71f, 0.71f, 0.71f),               // 1 - Common    (grey)
        new Color(0.40f, 0.80f, 0.40f),               // 2 - Uncommon  (green)
        new Color(0.28f, 0.55f, 0.90f),               // 3 - Rare      (blue)
        new Color(0.60f, 0.25f, 0.85f),               // 4 - Epic      (purple)
        new Color(0.98f, 0.80f, 0.10f),               // 5 - Legendary (gold)
        new Color(1f, 0.25f, 0f)                      // 6 - Unique    (orange)
    };

    private void Awake()
    {
        Deselect();
    }

    public void SetData(InventoryItemData data, int index)
    {
        ItemData = data;
        InventoryIndex = index;

        _itemImage.sprite = data.Item.Sprite;
        _itemImage.gameObject.SetActive(data.Item.Sprite != null);

        // Quantity only for stackable resources
        bool showQty = data.Item.ItemType == ItemType.Resource && data.Quantity > 1;
        _quantityText.gameObject.SetActive(showQty);
        if (showQty) _quantityText.text = data.Quantity.ToString();

        // Rarity Gradient
        int rarityIndex = Mathf.Clamp((int)data.Item.Rarity, 1, RarityColors.Length - 1);
        _rarityGradientImage.color = RarityColors[rarityIndex];

        // Equipment only fields
        bool isEquip = data.Item.ItemType == ItemType.Equipment;
        if (isEquip) _quantityText.text = $"+{data.UpgradeLevel}";
        //if (_upgradeLevelText != null)
        //{
        //    _upgradeLevelText.gameObject.SetActive(isEquip && data.UpgradeLevel > 0);
        //    if (isEquip && data.UpgradeLevel > 0)
        //        _upgradeLevelText.text = $"+{data.UpgradeLevel}";
        //}

        // Equipped badge
        if (_equippedBadge != null)
            _equippedBadge.SetActive(data.IsEquipped);
    }

    public void Clear()
    {
        _itemImage.sprite = null;
        _itemImage.gameObject.SetActive(false);
        _quantityText.gameObject.SetActive(false);
        _rarityGradientImage.color = Color.clear;
        if (_equippedBadge != null) _equippedBadge.SetActive(false);
        Deselect();
    }

    public void Select() => _selectionBorderImage.enabled = true;
    public void Deselect() => _selectionBorderImage.enabled = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            OnItemClicked?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData) => OnItemHovered?.Invoke(this);
    public void OnPointerExit(PointerEventData eventData) => OnItemUnhovered?.Invoke(this);
}
