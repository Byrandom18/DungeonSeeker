using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlotCell : MonoBehaviour
{
    [Header("Slot identity")]
    [SerializeField] private EquipmentSlot _slot;

    [Header("UI References")]
    [SerializeField] private Image _itemImage;
    [SerializeField] private Image _slotTypeIcon;      // empty shape
    [SerializeField] private Image _rarityGradient;
    //[SerializeField] private GameObject _equippedBadge;
    [SerializeField] private TMPro.TMP_Text _upgradeLevelText;
    //[SerializeField] private TMPro.TMP_Text _slotNameText;

    [Header("Data & Navigation")]
    [SerializeField] private InventorySO _inventorySO;
    [SerializeField] private InventoryPage _inventoryPage;

    private static readonly Color _emptyGradientColor = new Color(0.50f, 0.50f, 0.50f);

    private static readonly Color[] _rarityColors =
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
        // Button.onClick
        Button btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(OnCellClicked);

        //if (_slotNameText != null)
        //    _slotNameText.text = SlotDisplayName(_slot);
    }

    private void OnEnable()
    {
        if (_inventorySO != null)
            _inventorySO.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (_inventorySO != null)
            _inventorySO.OnInventoryChanged -= Refresh;
    }


    public void Refresh()
    {
        if (_inventorySO == null) return;

        int? idx = _inventorySO.GetEquippedIndex(_slot);

        if (idx.HasValue && idx.Value < _inventorySO.Items.Count)
        {
            InventoryItemData data = _inventorySO.Items[idx.Value];
            ShowItem(data);
        }
        else
        {
            ShowEmpty();
        }
    }

    private void ShowItem(InventoryItemData data)
    {
        if (_itemImage != null)
        {
            _itemImage.sprite = data.Item.Sprite;
            _itemImage.gameObject.SetActive(data.Item.Sprite != null);
        }

        if (_slotTypeIcon != null)
            _slotTypeIcon.gameObject.SetActive(false);

        if (_rarityGradient != null)
        {
            int ri = Mathf.Clamp((int)data.Rarity, 1, _rarityColors.Length - 1);
            _rarityGradient.color = _rarityColors[ri];
        }

        //if (_equippedBadge != null)
        //    _equippedBadge.SetActive(true);

        if (_upgradeLevelText != null)
        {
            bool hasUpgrade = data.UpgradeLevel > 0;
            _upgradeLevelText.gameObject.SetActive(hasUpgrade);
            if (hasUpgrade) _upgradeLevelText.text = $"+{data.UpgradeLevel}";
        }
    }

    private void ShowEmpty()
    {
        if (_itemImage != null)
            _itemImage.gameObject.SetActive(false);

        if (_slotTypeIcon != null)
            _slotTypeIcon.gameObject.SetActive(true);

        if (_rarityGradient != null)
            _rarityGradient.color = _emptyGradientColor;

        //if (_equippedBadge != null)
        //    _equippedBadge.SetActive(false);

        if (_upgradeLevelText != null)
            _upgradeLevelText.gameObject.SetActive(false);
    }


    private void OnCellClicked()
    {
        if (_inventoryPage == null) return;

        // Switch to the equipment tab and open the subsection of this slot
        _inventoryPage.SwitchToEquipmentSlot(_slot);
    }


    
}