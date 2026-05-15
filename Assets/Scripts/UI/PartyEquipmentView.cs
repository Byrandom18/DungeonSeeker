using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cycles through party members' equipment in the inventory UI.
/// Slot cells and equip actions use <see cref="SelectedEquipment"/>.
/// </summary>
public class PartyEquipmentView : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private Button _prevCharacterButton;
    [SerializeField] private Button _nextCharacterButton;
    [SerializeField] private TMP_Text _characterNameText;

    [Header("Slot cells (optional — auto-collected from children if empty)")]
    [SerializeField] private EquipmentSlotCell[] _slotCells;

    private readonly List<EquipmentComponent> _partyEquipment = new List<EquipmentComponent>();
    private int _selectedIndex;

    public EquipmentComponent SelectedEquipment =>
        _partyEquipment.Count > 0 ? _partyEquipment[_selectedIndex] : null;

    public string SelectedOwnerId => SelectedEquipment != null ? SelectedEquipment.PartyMemberId : null;

    public InventorySO SelectedInventory =>
        InventoryController.Instance != null
            ? InventoryController.Instance.GetInventorySO()
            : SelectedEquipment?.Inventory;

    public event Action OnSelectedCharacterChanged;

    private void Awake()
    {
        if (_slotCells == null || _slotCells.Length == 0)
            _slotCells = GetComponentsInChildren<EquipmentSlotCell>(true);

        _prevCharacterButton?.onClick.AddListener(SelectPreviousCharacter);
        _nextCharacterButton?.onClick.AddListener(SelectNextCharacter);
    }

    private void OnEnable()
    {
        if (PartyManager.Instance != null)
            PartyManager.Instance.OnPartyChanged += RebuildPartyList;

        RebuildPartyList();
    }

    private void OnDisable()
    {
        if (PartyManager.Instance != null)
            PartyManager.Instance.OnPartyChanged -= RebuildPartyList;
    }

    public void RebuildPartyList()
    {
        _partyEquipment.Clear();

        if (PartyManager.Instance != null)
        {
            foreach (var member in PartyManager.Instance.Members)
            {
                if (member?.Transform == null) continue;
                var equipment = member.Transform.GetComponent<EquipmentComponent>();
                if (equipment != null && !_partyEquipment.Contains(equipment))
                    _partyEquipment.Add(equipment);
            }
        }

        if (_partyEquipment.Count == 0)
        {
            var fallback = FindObjectsByType<EquipmentComponent>(FindObjectsSortMode.None);
            foreach (var equipment in fallback)
            {
                if (equipment != null && !_partyEquipment.Contains(equipment))
                    _partyEquipment.Add(equipment);
            }
        }

        SortPartyEquipment();
        _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, _partyEquipment.Count - 1));
        ApplySelection();
    }

    private void SortPartyEquipment()
    {
        _partyEquipment.Sort((a, b) =>
        {
            bool aPrimary = IsPrimaryPlayer(a);
            bool bPrimary = IsPrimaryPlayer(b);
            if (aPrimary != bPrimary) return aPrimary ? -1 : 1;
            return string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
        });
    }

    private static bool IsPrimaryPlayer(EquipmentComponent equipment)
    {
        var stats = equipment.GetComponent<PlayerStats>();
        return stats != null && stats.IsPrimaryPlayer;
    }

    public void SelectPreviousCharacter()
    {
        if (_partyEquipment.Count <= 1) return;
        _selectedIndex = (_selectedIndex - 1 + _partyEquipment.Count) % _partyEquipment.Count;
        ApplySelection();
    }

    public void SelectNextCharacter()
    {
        if (_partyEquipment.Count <= 1) return;
        _selectedIndex = (_selectedIndex + 1) % _partyEquipment.Count;
        ApplySelection();
    }

    private void ApplySelection()
    {
        bool hasMultiple = _partyEquipment.Count > 1;

        if (_prevCharacterButton != null)
            _prevCharacterButton.gameObject.SetActive(hasMultiple);
        if (_nextCharacterButton != null)
            _nextCharacterButton.gameObject.SetActive(hasMultiple);

        if (_characterNameText != null)
        {
            var selected = SelectedEquipment;
            _characterNameText.text = selected != null ? selected.DisplayName : string.Empty;
        }

        RefreshSlotCells();
        OnSelectedCharacterChanged?.Invoke();
    }

    private void RefreshSlotCells()
    {
        if (_slotCells == null) return;
        foreach (var cell in _slotCells)
        {
            if (cell != null)
                cell.Refresh();
        }
    }
}
