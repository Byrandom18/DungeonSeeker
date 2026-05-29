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

    [Header("Party equipment")]
    [SerializeField] private PartyEquipmentView _partyEquipmentView;

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
        BindInventory(_inventorySO);
        _inventorySO?.RebuildEquippedDictionary();

        if (_partyEquipmentView != null)
        {
            _partyEquipmentView.OnSelectedCharacterChanged += HandleSelectedCharacterChanged;
            _partyEquipmentView.RebuildPartyList();
        }
    }

    private void OnDisable()
    {
        UnbindInventory(_inventorySO);

        if (_partyEquipmentView != null)
            _partyEquipmentView.OnSelectedCharacterChanged -= HandleSelectedCharacterChanged;
    }

    private void HandleSelectedCharacterChanged()
    {
        RefreshCurrentView();
        if (_selectedItem != null)
            RefreshSelectedItemDescription();
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
        _partyEquipmentView?.RebuildPartyList();
        SwitchTab(_activeTab);          // re-apply current tab so list refreshes
    }

    public void HideInventory() => gameObject.SetActive(false);

    //Tab & filter =================================================================

    private void SwitchTab(Tab tab)
    {
        _activeTab = tab;
        _activeSlotFilter = null;
        bool isEquip = tab == Tab.Equipment;
        //if (_subFilterPanel) _subFilterPanel.SetActive(isEquip);
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

        // 1. Equipped on selected character
        // 2. Rarity
        // 3. Level
        // 4. ItemSO id
        indexed.Sort((a, b) =>
        {
            int eq = IsEquippedOnSelectedCharacter(b.sourceIdx).CompareTo(
                IsEquippedOnSelectedCharacter(a.sourceIdx));
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
                int sourceIdx = _currentViewSourceIndices[i];
                InventoryItemData cellData = _currentView[i];
                _spawnedItems[i].SetData(
                    cellData,
                    sourceIdx,
                    GetEquippedOwnerIcon(cellData));
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

        RefreshSelectedItemDescription();
    }

    private void RefreshSelectedItemDescription()
    {
        if (_selectedItem == null) return;

        int sourceIdx = _selectedItem.InventoryIndex;
        bool isEquip = _selectedItem.ItemData.Item.ItemType == ItemType.Equipment;

        _itemDescription.SetDescription(
            _inventorySO.Items[sourceIdx],
            isEquip ? () => HandleEquipButtonClicked(_selectedItem) : (System.Action)null,
            IsEquippedOnSelectedCharacter(sourceIdx)
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
        if (item == null || _inventorySO == null) return;

        int sourceIdx = item.InventoryIndex;
        if (sourceIdx < 0 || sourceIdx >= _inventorySO.Items.Count) return;

        string ownerId = GetSelectedOwnerId();
        if (string.IsNullOrEmpty(ownerId)) return;

        InventoryItemData data = _inventorySO.Items[sourceIdx];
        EquipmentSlot slot = data.Item.EquipmentSlot;

        if (IsEquippedOnSelectedCharacter(sourceIdx))
            _inventorySO.UnequipSlot(slot, ownerId);
        else
            _inventorySO.EquipItem(sourceIdx, ownerId);

        RefreshCurrentView();
        if (_selectedItem != null)
            RefreshSelectedItemDescription();
    }

    private string GetSelectedOwnerId()
    {
        if (_partyEquipmentView != null && _partyEquipmentView.SelectedEquipment != null)
            return _partyEquipmentView.SelectedEquipment.PartyMemberId;

        var allEquipment = FindObjectsByType<EquipmentComponent>(FindObjectsSortMode.None);
        foreach (var equipment in allEquipment)
        {
            var stats = equipment.GetComponent<PlayerStats>();
            if (stats != null && stats.IsPrimaryPlayer)
                return equipment.PartyMemberId;
        }

        return allEquipment.Length > 0 ? allEquipment[0].PartyMemberId : null;
    }

    private static Sprite GetEquippedOwnerIcon(InventoryItemData data)
    {
        if (!data.IsEquipped || string.IsNullOrEmpty(data.EquippedOwnerId))
            return null;

        return EquipmentComponent.GetIconForOwner(data.EquippedOwnerId);
    }

    private bool IsEquippedOnSelectedCharacter(int sharedInventoryIndex)
    {
        if (_inventorySO == null || sharedInventoryIndex < 0 || sharedInventoryIndex >= _inventorySO.Items.Count)
            return false;

        string ownerId = GetSelectedOwnerId();
        if (string.IsNullOrEmpty(ownerId))
            return false;

        return _inventorySO.IsEquippedOn(sharedInventoryIndex, ownerId);
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
        if (RunSystem.Instance != null && RunSystem.Instance.IsInRun)
        {
            _upgradeButton.interactable = false;
            return;
        }
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
            if (IsEquippedOnSelectedCharacter(_currentViewSourceIndices[i]))
                return _spawnedItems[i];
        }
        return _spawnedItems[0];
    }


    private void HandleUpgradeButtonClicked()
    {
        if (_selectedItem == null) return;
        if (RunSystem.Instance != null && RunSystem.Instance.IsInRun) return;

        int sourceIdx = _selectedItem.InventoryIndex;

        if (!_inventorySO.UpgradeItem(sourceIdx)) return;

        int updatedIdx = _selectedItem.InventoryIndex;

        RefreshSelectedItemDescription();
        RefreshUpgradeButton();
    }

    public void SetInventorySO(InventorySO inventorySO)
    {
        if (_inventorySO == inventorySO) return;

        bool wasEnabled = isActiveAndEnabled;
        if (wasEnabled)
            UnbindInventory(_inventorySO);

        _inventorySO = inventorySO;
        _selectedItem = null;
        _itemDescription.ResetDescription();

        if (wasEnabled)
            BindInventory(_inventorySO);

        if (gameObject.activeInHierarchy)
            ReSortAndRefresh();
    }

    private void BindInventory(InventorySO inventory)
    {
        if (inventory == null) return;
        inventory.OnInventoryChanged += RefreshCurrentView;
        inventory.OnInventoryChanged += RefreshUpgradeButton;
        inventory.OnItemAdded += OnItemAdded;
        inventory.OnItemRemovedAt += OnItemRemovedAt;
    }

    private void UnbindInventory(InventorySO inventory)
    {
        if (inventory == null) return;
        inventory.OnInventoryChanged -= RefreshCurrentView;
        inventory.OnInventoryChanged -= RefreshUpgradeButton;
        inventory.OnItemAdded -= OnItemAdded;
        inventory.OnItemRemovedAt -= OnItemRemovedAt;
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
