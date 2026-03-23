using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlotsUI : MonoBehaviour
{
    [SerializeField] private InventorySO _inventorySO;
    [SerializeField] private EquipmentSlotCell[] _slotCells; // one per slot, same order as EquipmentSlot enum

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

    private void Refresh()
    {
        EquipmentSlot[] slots = (EquipmentSlot[])Enum.GetValues(typeof(EquipmentSlot));

        for (int i = 0; i < _slotCells.Length; i++)
        {
            EquipmentSlotCell cell = _slotCells[i];
            if (cell == null) continue;

            EquipmentSlot slot = i + 1 < slots.Length ? slots[i + 1] : EquipmentSlot.None;
            int? equippedIdx = _inventorySO.GetEquippedIndex(slot);

            if (equippedIdx.HasValue && equippedIdx.Value < _inventorySO.Items.Count)
            {
                InventoryItemData data = _inventorySO.Items[equippedIdx.Value];
                cell.SetItem(data.Item.Sprite, data.Item.Name, data.UpgradeLevel);
            }
            else
            {
                cell.Clear(slot.ToString());
            }
        }
    }
}


[Serializable]
public class EquipmentSlotCell
{
    public Image ItemImage;
    public TMP_Text SlotLabel;
    public TMP_Text UpgradeLevelText;

    public void SetItem(Sprite sprite, string itemName, int upgradeLevel)
    {
        ItemImage.sprite = sprite;
        ItemImage.gameObject.SetActive(sprite != null);
        SlotLabel.text = itemName;
        UpgradeLevelText.text = "+" + upgradeLevel;
    }

    public void Clear(string slotName)
    {
        ItemImage.sprite = null;
        ItemImage.gameObject.SetActive(false);
        SlotLabel.text = slotName;
        UpgradeLevelText.gameObject.SetActive(false);
    }
}