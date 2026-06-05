using UnityEngine;

/// <summary>
/// Manages the full ability bar.
/// Drag this onto the root UI panel that contains all slot prefabs.
///
/// Setup:
///   1. Create a child HorizontalLayoutGroup with N AbilitySlotUI children.
///   2. Assign those children to _slots[] in the Inspector (same order as AbilitySystem._abilitySlots).
///   3. Assign the target character's AbilitySystem to _abilitySystem,
///      OR leave it null and call Initialize() from your player-setup code.
/// </summary>
public class AbilityBarUI : MonoBehaviour
{
    [SerializeField] private AbilitySlotUI[] _slots;
    [SerializeField] private AbilitySystem _abilitySystem;

    // == Unity lifecycle ==========================================

    private void Start()
    {
        if (_abilitySystem != null)
            Initialize(_abilitySystem);
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    // == Public API ===============================================

    /// <summary>
    /// Call this to bind the bar to a specific AbilitySystem at runtime
    /// (e.g. after the player spawns).
    /// </summary>
    public void Initialize(AbilitySystem system)
    {
        Unsubscribe();
        _abilitySystem = system;

        if (_abilitySystem == null) return;

        // Populate initial slot data
        RefreshAllSlots();

        // Subscribe to events
        _abilitySystem.OnCooldownChanged += HandleCooldownChanged;
        _abilitySystem.OnAbilityUsed += HandleAbilityUsed;
    }

    // == Private helpers ==========================================

    /// <summary>
    /// Re-reads all AbilitySO data from the system and updates slot visuals.
    /// Safe to call at any time (e.g. after swapping an ability from the inventory).
    /// </summary>
    private void RefreshAllSlots()
    {
        if (_abilitySystem == null || _slots == null) return;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null) continue;

            // AbilitySystem exposes the raw SO array via a public accessor we'll add below.
            AbilitySO data = _abilitySystem.GetAbilityData(i);
            _slots[i].SetAbility(data, i);
        }
    }

    private void HandleCooldownChanged(int slotIndex, float remaining)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Length) return;
        _slots[slotIndex]?.SetCooldown(remaining);
    }

    private void HandleAbilityUsed(int slotIndex)
    {
        // Optional: trigger a "flash" or "pulse" animation on the slot.
        // For now the cooldown overlay handles the visual feedback automatically.
    }

    private void Unsubscribe()
    {
        if (_abilitySystem == null) return;
        _abilitySystem.OnCooldownChanged -= HandleCooldownChanged;
        _abilitySystem.OnAbilityUsed -= HandleAbilityUsed;
    }
}
