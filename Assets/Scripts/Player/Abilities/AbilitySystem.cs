using System;
using UnityEngine;

public class AbilitySystem : MonoBehaviour
{
    [SerializeField] private AbilitySO[] _abilitySlots; // size = number of slots (4)

    private AbilityBase[] _abilities;
    private ICharacterEntity _owner;
    [SerializeField] private ActiveWeapon _activeWeapon;

    public int AbilitySlotCount => _abilities != null ? _abilities.Length : 0;

    public event Action<int, float> OnCooldownChanged;
    public event Action<int> OnAbilityUsed;

    // raised when an ability SO is swapped so the UI can refresh the slot.
    public event Action<int, AbilitySO> OnAbilityChanged;

    private void Awake()
    {
        _owner = GetComponent<ICharacterEntity>();
        _abilities = new AbilityBase[_abilitySlots.Length];
        for (int i = 0; i < _abilitySlots.Length; i++)
            if (_abilitySlots[i] != null)
                _abilities[i] = AbilityFactory.Create(_abilitySlots[i], _owner);
    }

    private void Update()
    {
        for (int i = 0; i < _abilities.Length; i++)
        {
            if (_abilities[i] == null) continue;
            _abilities[i].Tick(Time.deltaTime);
            OnCooldownChanged?.Invoke(i, _abilities[i].Cooldown);
        }
    }

    public bool UseAbility(int slotIndex, Vector3 aimPosition)
    {
        if (slotIndex < 0 || slotIndex >= _abilities.Length) return false;
        if (_abilities[slotIndex] == null) return false;

        var ctx = new AbilityContext
        {
            AimPosition = aimPosition,
            Owner = _owner,
            ActiveWeapon = _activeWeapon?.GetActiveWeapon()
        };

        bool used = _abilities[slotIndex].TryActivate(ctx);
        if (used) OnAbilityUsed?.Invoke(slotIndex);
        return used;
    }

    public bool CanUseSlot(int slotIndex, ActiveWeapon activeWeapon)
    {
        if (slotIndex < 0 || slotIndex >= _abilities.Length) return false;
        if (_abilities[slotIndex] == null) return false;

        AbilitySO data = _abilitySlots[slotIndex];
        if (data == null) return false;
        if (_owner != null && _owner is PlayerStats stats && stats.Mana < data.ManaCost)
            return false;

        var ctx = new AbilityContext
        {
            AimPosition = transform.position,
            Owner = _owner,
            ActiveWeapon = activeWeapon != null ? activeWeapon.GetActiveWeapon() : _activeWeapon?.GetActiveWeapon()
        };

        return _abilities[slotIndex].CanActivateWithoutCost(ctx);
    }

    public bool IsSlotReady(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _abilities.Length) return false;
        return _abilities[slotIndex]?.IsReady == true;
    }

    /// <summary>
    /// Returns the ScriptableObject for a given slot (null if slot is empty).
    /// Used by AbilityBarUI to read icon / mana cost / display name.
    /// </summary>
    public AbilitySO GetAbilityData(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _abilitySlots.Length) return null;
        return _abilitySlots[slotIndex];
    }

    /// <summary>
    /// Hot-swap an ability from the inventory / UI drag-and-drop.
    /// Fires OnAbilityChanged so the bar can refresh that slot immediately.
    /// </summary>
    public void SetAbility(int slotIndex, AbilitySO abilitySO)
    {
        if (slotIndex < 0 || slotIndex >= _abilities.Length) return;

        _abilitySlots[slotIndex] = abilitySO;
        _abilities[slotIndex] = abilitySO != null
            ? AbilityFactory.Create(abilitySO, _owner)
            : null;

        OnAbilityChanged?.Invoke(slotIndex, abilitySO);
    }
}
