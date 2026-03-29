using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPage : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private InventorySO _inventorySO;

    [Header("Item Grid")]
    [SerializeField] private InventoryItem _itemPrefab;
    [SerializeField] private RectTransform _contentPanel;

    [Header("Description")]
    [SerializeField] private InventoryDescription _itemDescription;

    [Header("Main Tabs")]
    [SerializeField] private Button _resourceTabButton;
    [SerializeField] private Button _equipmentTabButton;

    [Header("Equipment Sub-filter")]
    [SerializeField] private GameObject _subFilterPanel;          // parent containing slot buttons
    //[SerializeField] private Button _allEquipmentButton;
    //[SerializeField] private Button[] _slotFilterButtons;         // order matches EquipmentSlot enum (skip None)

    [Header("Action Buttons")]
    [SerializeField] private Button _equipButton;
    [SerializeField] private Button _upgradeButton;

    // Internal state
    private readonly List<InventoryItem> _spawnedItems = new List<InventoryItem>();
    private List<InventoryItemData> _currentView = new List<InventoryItemData>();
    private List<int> _currentViewSourceIndices = new List<int>(); // maps _currentView[i] -> _inventorySO.Items[j]

    private InventoryItem _selectedItem;

    private enum Tab { Resources, Equipment }
    private Tab _activeTab = Tab.Equipment;
    private EquipmentSlot? _activeSlotFilter = null; // null = all equipment

    private void Awake()
    {
        HideInventory();
        _itemDescription.ResetDescription();
        SetActionButtonsVisible(false);
        BindTabButtons();
        //BindSlotButtons();
    }

    private void OnEnable()
    {
        if (_inventorySO != null)
            _inventorySO.OnInventoryChanged += RefreshCurrentView;

        _inventorySO?.RebuildEquippedDictionary();
    }

    private void OnDisable()
    {
        if (_inventorySO != null)
            _inventorySO.OnInventoryChanged -= RefreshCurrentView;
    }

    public void ShowInventory()
    {
        gameObject.SetActive(true);
        _itemDescription.ResetDescription();
        _selectedItem = null;
        SwitchTab(_activeTab);          // re-apply current tab so list refreshes
    }

    public void HideInventory() => gameObject.SetActive(false);

    private void SwitchTab(Tab tab)
    {
        _activeTab = tab;
        _activeSlotFilter = null;
        bool isEquip = tab == Tab.Equipment;
        if (_subFilterPanel) _subFilterPanel.SetActive(isEquip);
        _selectedItem?.Deselect();
        _selectedItem = null;
        RefreshCurrentView();
    }

    private void SwitchSlotFilter(EquipmentSlot? slot)
    {
        DeselectCurrent();
        _activeSlotFilter = slot;
        RefreshCurrentView();
    }

    public void SwitchToEquipmentSlot(EquipmentSlot slot)
    {
        _activeTab = Tab.Equipment;
        if (_subFilterPanel) _subFilterPanel.SetActive(true);
        _activeSlotFilter = slot;
        _selectedItem?.Deselect();
        _selectedItem = null;
        RefreshCurrentView();
    }

    private void RefreshCurrentView()
    {
        if (_inventorySO == null) return;

        // Build view list with original indices
        _currentView.Clear();
        _currentViewSourceIndices.Clear();

        List<InventoryItemData> sorted = _activeTab == Tab.Resources
            ? _inventorySO.GetResourcesSorted()
            : _inventorySO.GetEquipmentSorted(_activeSlotFilter);

        // Map back to source indices for equip calls
        IReadOnlyList<InventoryItemData> source = _inventorySO.Items;
        foreach (var data in sorted)
        {
            // Find first matching index in source
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i].Item == data.Item && source[i].UpgradeLevel == data.UpgradeLevel)
                {
                    _currentView.Add(data);
                    _currentViewSourceIndices.Add(i);
                    break;
                }
            }
        }

        RepopulateGrid();
    }

    private void RepopulateGrid()
    {
        // Grow pool as needed
        while (_spawnedItems.Count < _currentView.Count)
        {
            InventoryItem cell = Instantiate(_itemPrefab, Vector3.zero, Quaternion.identity);
            cell.transform.SetParent(_contentPanel, false);
            _spawnedItems.Add(cell);
            cell.OnItemClicked += HandleItemClicked;
            cell.OnItemHovered += HandleItemHovered;
            cell.OnItemUnhovered += HandleItemUnhovered;
        }

        // Fill / clear
        for (int i = 0; i < _spawnedItems.Count; i++)
        {
            if (i < _currentView.Count)
            {
                _spawnedItems[i].gameObject.SetActive(true);
                _spawnedItems[i].SetData(_currentView[i], _currentViewSourceIndices[i]);
            }
            else
            {
                _spawnedItems[i].gameObject.SetActive(false);
                _spawnedItems[i].Clear();
            }
        }

        if (_currentView.Count == 0) return;

        // Refresh selection highlight
        bool selectedStillValid = _selectedItem != null
        && _spawnedItems.IndexOf(_selectedItem) is int idx
        && idx >= 0 && idx < _currentView.Count;

        if (!selectedStillValid)
        {
            _selectedItem?.Deselect();
            _selectedItem = null;
        }

        if (_selectedItem == null)
            SelectItem(FindEquippedCellOrFirst());
    }

    private void HandleItemClicked(InventoryItem item) => SelectItem(item);

    private void SelectItem(InventoryItem item)
    {
        _selectedItem?.Deselect();
        _selectedItem = item;
        item.Select();

        bool isEquip = item.ItemData.Item.ItemType == ItemType.Equipment;
        SetActionButtonsVisible(isEquip);

        _itemDescription.SetDescription(
            item.ItemData,
            isEquip ? () => HandleEquipButtonClicked(item) : (System.Action)null
        );
    }

    private void DeselectCurrent()
    {
        _selectedItem?.Deselect();
        _selectedItem = null;
        _itemDescription.ResetDescription();
        SetActionButtonsVisible(false);
    }

    private void HandleEquipButtonClicked(InventoryItem item)
    {
        if (item == null) return;

        int sourceIdx = item.InventoryIndex;
        if (sourceIdx < 0 || sourceIdx >= _inventorySO.Items.Count) return;
        InventoryItemData data = _inventorySO.Items[sourceIdx];

        if (data.IsEquipped)
            _inventorySO.UnequipSlot(data.Item.EquipmentSlot);
        else
            _inventorySO.EquipItem(sourceIdx);

        // Description panel re-binds via OnInventoryChanged -> RefreshCurrentView
        // But we also immediately update the description to reflect equip state
        if (_selectedItem != null)
        {
            // Re-fetch updated data
            _itemDescription.SetDescription(
                _inventorySO.Items[sourceIdx],
                () => HandleEquipButtonClicked(_selectedItem)
            );
        }
    }

    private void HandleItemHovered(InventoryItem item) { /* Optional: tooltip */ }
    private void HandleItemUnhovered(InventoryItem item) { /* Optional: hide tooltip */ }

    private void SetActionButtonsVisible(bool visible)
    {
        if (_equipButton != null) _equipButton.gameObject.SetActive(visible);
        if (_upgradeButton != null) _upgradeButton.gameObject.SetActive(visible);
    }


    private void BindTabButtons()
    {
        _resourceTabButton?.onClick.AddListener(() => SwitchTab(Tab.Resources));
        _equipmentTabButton?.onClick.AddListener(() => SwitchTab(Tab.Equipment));
    }

    private InventoryItem FindEquippedCellOrFirst()
    {
        for (int i = 0; i < _currentView.Count; i++)
        {
            if (_currentView[i].IsEquipped)
                return _spawnedItems[i];
        }
        return _spawnedItems[0];
    }

    //private void BindSlotButtons()
    //{
    //    //_allEquipmentButton?.onClick.AddListener(() => SwitchSlotFilter(null));

    //    // EquipmentSlot enum 
    //    EquipmentSlot[] slots = (EquipmentSlot[])Enum.GetValues(typeof(EquipmentSlot));
    //    for (int i = 0; i < _slotFilterButtons.Length; i++)
    //    {
    //        if (_slotFilterButtons[i] == null) continue;
    //        EquipmentSlot slot = i + 1 < slots.Length ? slots[i + 1] : EquipmentSlot.None;
    //        if (slot == EquipmentSlot.None) continue;
    //        int captured = i;
    //        _slotFilterButtons[captured].onClick.AddListener(() => SwitchSlotFilter(slots[captured + 1]));
    //    }
    //}

    private void OnDestroy()
    {
        foreach (var cell in _spawnedItems)
        {
            if (cell == null) continue;
            cell.OnItemClicked -= HandleItemClicked;
            cell.OnItemHovered -= HandleItemHovered;
            cell.OnItemUnhovered -= HandleItemUnhovered;
        }
    }
}
