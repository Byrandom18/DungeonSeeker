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
    }

    private void OnEnable()
    {
        if (_inventorySO != null)
        {
            _inventorySO.OnInventoryChanged += RefreshCurrentView;
            _inventorySO.OnInventoryChanged += RefreshUpgradeButton;
            _inventorySO.OnItemAdded += OnItemAdded;
            _inventorySO.OnItemRemovedAt += OnItemRemovedAt;
        }

        _inventorySO?.RebuildEquippedDictionary();
    }

    private void OnDisable()
    {
        if (_inventorySO != null)
        {
            _inventorySO.OnInventoryChanged -= RefreshCurrentView;
            _inventorySO.OnInventoryChanged -= RefreshUpgradeButton;
            _inventorySO.OnItemAdded -= OnItemAdded;
            _inventorySO.OnItemRemovedAt -= OnItemRemovedAt;
        }
    }


    private void OnItemAdded() => ReSortAndRefresh();


    private void OnItemRemovedAt(int removedIndex)
    {
        for (int i = _currentViewSourceIndices.Count - 1; i >= 0; i--)
        {
            if (_currentViewSourceIndices[i] == removedIndex)
            {
                _currentView.RemoveAt(i);
                _currentViewSourceIndices.RemoveAt(i);
            }
            else if (_currentViewSourceIndices[i] > removedIndex)
            {
                _currentViewSourceIndices[i]--;
            }
        }
    }

    //Show/hide

    public void ShowInventory()
    {
        gameObject.SetActive(true);
        _itemDescription.ResetDescription();
        _selectedItem = null;
        SwitchTab(_activeTab);          // re-apply current tab so list refreshes
    }

    public void HideInventory() => gameObject.SetActive(false);

    //Tab & filter =================================================================

    private void SwitchTab(Tab tab)
    {
        _activeTab = tab;
        _activeSlotFilter = null;
        bool isEquip = tab == Tab.Equipment;
        if (_subFilterPanel) _subFilterPanel.SetActive(isEquip);
        _selectedItem?.Deselect();
        _selectedItem = null;
        ReSortAndRefresh();
    }

    private void SwitchSlotFilter(EquipmentSlot? slot)
    {
        DeselectCurrent();
        _activeSlotFilter = slot;
        ReSortAndRefresh();
    }

    public void SwitchToEquipmentSlot(EquipmentSlot slot)
    {
        _activeTab = Tab.Equipment;
        if (_subFilterPanel) _subFilterPanel.SetActive(true);
        _activeSlotFilter = slot;
        _selectedItem?.Deselect();
        _selectedItem = null;
        ReSortAndRefresh();
    }

    //Refresh data ================================================================

    private void ReSortAndRefresh()
    {
        if (_inventorySO == null) return;

        _currentView.Clear();
        _currentViewSourceIndices.Clear();

        IReadOnlyList<InventoryItemData> source = _inventorySO.Items;
        var indexed = new List<(int sourceIdx, InventoryItemData data)>();

        for (int i = 0; i < source.Count; i++)
        {
            var data = source[i];
            if (_activeTab == Tab.Resources && data.Item.ItemType != ItemType.Resource) continue;
            if (_activeTab == Tab.Equipment && data.Item.ItemType != ItemType.Equipment) continue;
            if (_activeTab == Tab.Equipment && _activeSlotFilter.HasValue
                && data.Item.EquipmentSlot != _activeSlotFilter.Value) continue;
            indexed.Add((i, data));
        }

        // 1. Equipped
        // 2. Rarity
        // 3. Level
        // 4. ItemSO id
        indexed.Sort((a, b) =>
        {
            int eq = b.data.IsEquipped.CompareTo(a.data.IsEquipped);
            if (eq != 0) return eq;

            int rar = ((int)b.data.Rarity).CompareTo((int)a.data.Rarity);
            if (rar != 0) return rar;

            int lvl = b.data.UpgradeLevel.CompareTo(a.data.UpgradeLevel);
            if (lvl != 0) return lvl;

            return a.data.Item.GetInstanceID().CompareTo(b.data.Item.GetInstanceID());
        });

        foreach (var (sourceIdx, data) in indexed)
        {
            _currentView.Add(data);
            _currentViewSourceIndices.Add(sourceIdx);
        }

        RepopulateGrid();
    }

    private void RefreshCurrentView()
    {
        if (_inventorySO == null) return;

        IReadOnlyList<InventoryItemData> source = _inventorySO.Items;

        for (int i = 0; i < _currentViewSourceIndices.Count; i++)
            _currentView[i] = source[_currentViewSourceIndices[i]];

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
        bool selectedStillValid = false;
        if (_selectedItem != null)
        {
            int idx = _spawnedItems.IndexOf(_selectedItem);
            selectedStillValid = idx >= 0 && idx < _currentView.Count;
        }

        if (!selectedStillValid)
        {
            _selectedItem?.Deselect();
            _selectedItem = null;
        }

        if (_selectedItem == null)
            SelectItem(FindEquippedCellOrFirst());
    }

    //Selection =======================================================================

    private void HandleItemClicked(InventoryItem item) => SelectItem(item);

    private void SelectItem(InventoryItem item)
    {
        _selectedItem?.Deselect();
        _selectedItem = item;
        item.Select();

        bool isEquip = item.ItemData.Item.ItemType == ItemType.Equipment;
        SetActionButtonsVisible(isEquip);

        RefreshUpgradeButton();

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

    //Equip ===================================================================================

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

    private void RefreshUpgradeButton()
    {
        if (_upgradeButton == null) return;
        if (_selectedItem == null)
        {
            _upgradeButton.interactable = false;
            return;
        }
        int sourceIdx = _selectedItem.InventoryIndex;
        _upgradeButton.interactable = _inventorySO.CanUpgradeItem(sourceIdx);
    }

    private void BindTabButtons()
    {
        _resourceTabButton?.onClick.AddListener(() => SwitchTab(Tab.Resources));
        _equipmentTabButton?.onClick.AddListener(() => SwitchTab(Tab.Equipment));
        _upgradeButton?.onClick.AddListener(HandleUpgradeButtonClicked);
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


    private void HandleUpgradeButtonClicked()
    {
        if (_selectedItem == null) return;

        int sourceIdx = _selectedItem.InventoryIndex;

        if (!_inventorySO.UpgradeItem(sourceIdx)) return;

        int updatedIdx = _selectedItem.InventoryIndex;

        _itemDescription.SetDescription(
            _inventorySO.Items[updatedIdx],
            () => HandleEquipButtonClicked(_selectedItem)
        );
        RefreshUpgradeButton();
    }

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
