using System;
using UnityEngine;

public class AbilitySystem : MonoBehaviour
{
    // —лоты способностей Ч задаютс€ в Inspector через SO
    [SerializeField] private AbilitySO[] _abilitySlots; // размер = кол-во слотов (4)

    private AbilityBase[] _abilities;
    private ICharacterEntity _owner;
    [SerializeField] private ActiveWeapon _activeWeapon; // null у ботов

    public event Action<int, float> OnCooldownChanged; // слот, оставшийс€ кулдаун
    public event Action<int> OnAbilityUsed;

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

    // —мена способности в слоте (из UI выбора способностей)
    public void SetAbility(int slotIndex, AbilitySO abilitySO)
    {
        if (slotIndex < 0 || slotIndex >= _abilities.Length) return;
        _abilitySlots[slotIndex] = abilitySO;
        _abilities[slotIndex] = abilitySO != null
            ? AbilityFactory.Create(abilitySO, _owner)
            : null;
    }
}
