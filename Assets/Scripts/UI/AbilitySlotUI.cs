using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

/// <summary>
/// Manages the visual state of a single ability slot:
/// icon, cooldown overlay + timer text, and mana cost label.
/// Attach this to the root GameObject of one slot prefab.
/// </summary>
public class AbilitySlotUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _cooldownOverlay;   // radial fill Image (fill type = Radial360)
    [SerializeField] private TextMeshProUGUI _cooldownText;
    [SerializeField] private TextMeshProUGUI _manaCostText;
    [SerializeField] private TextMeshProUGUI _hotkeyText;
    [SerializeField] private GameObject _emptySlotOverlay; // shown when no ability assigned

    private float _maxCooldown;

    // == Public API ==============================================

    /// <summary>Bind slot data from an AbilitySO. Pass null to show an empty slot.</summary>
    public void SetAbility(AbilitySO data, int slotIndex)
    {
        bool hasAbility = data != null;

        if (_emptySlotOverlay != null)
            _emptySlotOverlay.SetActive(!hasAbility);

        if (_iconImage != null)
        {
            _iconImage.sprite = hasAbility ? data.Icon : null;
            _iconImage.enabled = hasAbility;
        }

        if (_manaCostText != null)
            _manaCostText.text = hasAbility && data.ManaCost > 0
                ? $"{data.ManaCost:0}"
                : string.Empty;

        if (_hotkeyText != null)
            _hotkeyText.text = SlotIndexToHotkey(slotIndex);

        _maxCooldown = hasAbility ? data.Cooldown : 0f;

        // Reset cooldown visuals
        SetCooldown(0f);
    }

    /// <summary>
    /// Called every frame by AbilityBarUI with the remaining cooldown value
    /// (0 = ready, >0 = on cooldown).
    /// </summary>
    public void SetCooldown(float remaining)
    {
        bool onCooldown = remaining > 0.01f;

        if (_cooldownOverlay != null)
        {
            _cooldownOverlay.enabled = onCooldown;
            _cooldownOverlay.fillAmount = (_maxCooldown > 0f)
                ? remaining / _maxCooldown
                : 0f;
        }

        if (_cooldownText != null)
        {
            _cooldownText.enabled = onCooldown;
            _cooldownText.text = onCooldown ? $"{remaining:0.0}" : string.Empty;
        }

        // Dim the icon while on cooldown
        if (_iconImage != null)
            _iconImage.color = onCooldown
                ? new Color(0.4f, 0.4f, 0.4f, 1f)
                : Color.white;
    }

    // == Helpers =================================================

    private string SlotIndexToHotkey(int index)
    {
        InputAction action = GameInput.Instance.GetAbilityAction(index);
        if (action == null) return "[?]";

        if (action.controls.Count > 0 && action.controls[0] is KeyControl keyControl)
        {
            string keyCode = GameUtils.Utils.GetEnglishKeyName(keyControl.keyCode);
            return string.IsNullOrEmpty(keyCode) ? "[-]" : keyCode;
        }

        return "[-]";
    }

    
}
