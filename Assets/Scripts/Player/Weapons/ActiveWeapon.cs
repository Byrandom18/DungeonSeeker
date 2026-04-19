using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the character's active weapon.
///
/// Stores one WeaponBase component for each WeaponType.
/// When equipping an item with a WeaponSO:
/// 1. Activates the desired WeaponBase by the WeaponType
/// 2. Transfers the weapon to it (sprite, damage, cooldown, projectile parameters)
/// 3. Passes a link to the owner (ICharacterEntity)
///
/// When removing the weapon, deactivates all WeaponBase.
/// </summary>
public class ActiveWeapon : MonoBehaviour
{
    public static ActiveWeapon Instance { get; private set; }

    [Header("Owner")]
    [SerializeField] private PlayerStats _ownerStats;  // or AllyStats — any ICharacterEntity

    [Header("Weapon components (by 1 on each WeaponType)")]
    [SerializeField] private WeaponEntry[] _weaponEntries;

    [Header("Inventory")]
    [SerializeField] private InventorySO _inventorySO;

    // current active weapon
    private WeaponBase _activeWeapon;

    private bool _isMidSwing;
    private WeaponSO _pendingWeaponSO; // SO которое нужно применить после атаки
    private bool _pendingDeactivate;

    public bool RotationEnabled
    {
        get => _activeWeapon != null && _activeWeapon.RotationEnabled;
        set
        {
            if (_activeWeapon != null)
                _activeWeapon.RotationEnabled = value;
        }
    }

    private Dictionary<WeaponType, WeaponBase> _weaponMap;

    [System.Serializable]
    public struct WeaponEntry
    {
        public WeaponType Type;
        public WeaponBase Weapon;
    }

    private void Awake()
    {
        Instance = this;

        _weaponMap = new Dictionary<WeaponType, WeaponBase>();
        foreach (var entry in _weaponEntries)
        {
            if (entry.Weapon != null)
                _weaponMap[entry.Type] = entry.Weapon;
        }
    }

    private void OnEnable()
    {
        if (_inventorySO != null)
            _inventorySO.OnInventoryChanged += OnInventoryChanged;
    }

    private void OnDisable()
    {
        if (_inventorySO != null)
            _inventorySO.OnInventoryChanged -= OnInventoryChanged;
    }

    private void Start()
    {
        foreach (var kv in _weaponMap)
            kv.Value.gameObject.SetActive(false);
        OnInventoryChanged(); // apply the current inventory status at start
    }

    private void Update()
    {
        if (_activeWeapon == null) return;
        FollowMousePosition();
    }

    public WeaponBase GetActiveWeapon() => _activeWeapon;

    public void NotifyAttackStarted() => _isMidSwing = true;

    public void NotifyAttackEnded()
    {
        _isMidSwing = false;
        ApplyPendingWeaponChange();
    }

    private void OnInventoryChanged()
    {
        if (_inventorySO == null) return;

        WeaponSO equippedSO = null;
        foreach (var data in _inventorySO.Items)
        {
            if (data.IsEquipped
                && data.Item != null
                && data.Item.EquipmentSlot == EquipmentSlot.Weapon
                && data.Item.WeaponSO != null)
            {
                equippedSO = data.Item.WeaponSO;
                break;
            }
        }

        if (_isMidSwing)
        {
            _pendingWeaponSO = equippedSO;
            _pendingDeactivate = (equippedSO == null);
            return;
        }

        if (equippedSO != null) ActivateWeapon(equippedSO);
        else DeactivateAll();
    }

    private void ApplyPendingWeaponChange()
    {
        if (_pendingWeaponSO == null && !_pendingDeactivate) return;

        if (_pendingDeactivate)
            DeactivateAll();
        else
            ActivateWeapon(_pendingWeaponSO);

        _pendingWeaponSO = null;
        _pendingDeactivate = false;
    }

    private void ActivateWeapon(WeaponSO weaponData)
    {
        // Деактивировать предыдущее
        if (_activeWeapon != null)
            _activeWeapon.gameObject.SetActive(false);

        if (!_weaponMap.TryGetValue(weaponData.WeaponType, out WeaponBase weapon))
        {
            Debug.LogWarning($"[ActiveWeapon] no component for the WeaponType {weaponData.WeaponType}");
            _activeWeapon = null;
            return;
        }

        _activeWeapon = weapon;
        _activeWeapon.Owner = _ownerStats;   // ICharacterEntity
        _activeWeapon.ApplyWeaponSO(weaponData);
        _activeWeapon.gameObject.SetActive(true);
    }

    private void DeactivateAll()
    {
        if (_activeWeapon != null)
        {
            if (_activeWeapon is Sword sword) sword.EndAttack();
            _activeWeapon.gameObject.SetActive(false);
        }
        _activeWeapon = null;
    }

    private void FollowMousePosition()
    {
        if (!RotationEnabled) return;

        Vector3 mousePos = GameInput.Instance.GetMousePosition();
        Vector3 playerScreen = PlayerMovement.Instance.GetPlayerScreenPosition();
        Vector3 direction = mousePos - playerScreen;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
