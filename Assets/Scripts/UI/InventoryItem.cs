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
    [SerializeField] private Image _equippedOwnerIcon;

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
        if (_equippedBadge != null && _equippedOwnerIcon == null)
            _equippedOwnerIcon = _equippedBadge.GetComponent<Image>();

        Deselect();
    }

    public void SetData(InventoryItemData data, int index, Sprite equippedOwnerIcon = null)
    {
        ItemData = data;
        InventoryIndex = index;

        _itemImage.sprite = data.Item.Sprite;
        _itemImage.gameObject.SetActive(data.Item.Sprite != null);

        bool isEquip = data.Item.ItemType == ItemType.Equipment;
        _quantityText.gameObject.SetActive(true);
        if (isEquip)
        {
            _quantityText.text = $"+{data.UpgradeLevel}";
        }
        else
        {
            _quantityText.text = data.Quantity.ToString();
        }

        int rarityIndex = Mathf.Clamp((int)data.Rarity, 1, RarityColors.Length - 1);
        _rarityGradientImage.color = RarityColors[rarityIndex];

        bool isEquippedOnAnyone = data.IsEquipped && !string.IsNullOrEmpty(data.EquippedOwnerId);
        if (_equippedBadge != null)
            _equippedBadge.SetActive(isEquippedOnAnyone);

        if (_equippedOwnerIcon != null)
        {
            if (isEquippedOnAnyone && equippedOwnerIcon != null)
            {
                _equippedOwnerIcon.sprite = equippedOwnerIcon;
                _equippedOwnerIcon.enabled = true;
            }
            else
            {
                _equippedOwnerIcon.sprite = null;
                _equippedOwnerIcon.enabled = false;
            }
        }
    }

    public void Clear()
    {
        _itemImage.sprite = null;
        _itemImage.gameObject.SetActive(false);
        _quantityText.gameObject.SetActive(false);
        _rarityGradientImage.color = Color.clear;
        if (_equippedBadge != null) _equippedBadge.SetActive(false);
        if (_equippedOwnerIcon != null)
        {
            _equippedOwnerIcon.sprite = null;
            _equippedOwnerIcon.enabled = false;
        }
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
