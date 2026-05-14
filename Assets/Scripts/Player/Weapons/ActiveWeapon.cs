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
    /// <summary>Singleton for the primary player prefab only; allies leave this unset.</summary>
    public static ActiveWeapon Instance { get; private set; }

    /// <summary>When enabled, assigns <see cref="Instance"/> on Awake and clears OnDestroy.</summary>
    [SerializeField] private bool _registerPlayerSingleton = true;

    /// <summary>Mouse aim uses <see cref="GameInput"/> and <see cref="PlayerMovement"/> (primary player).</summary>
    public enum AimMode
    {
        MouseRelativeToPlayer,
        AimAtWorldTarget
    }

    [Header("Aiming")]
    [SerializeField] private AimMode _aimMode = AimMode.MouseRelativeToPlayer;
    [SerializeField] private Transform _worldAimTarget;

    [Header("Owner")]
    [SerializeField] private PlayerStats _ownerStats;

    [Header("Weapon components (by 1 on each WeaponType)")]
    [SerializeField] private WeaponEntry[] _weaponEntries;

    [Header("Inventory")]
    [SerializeField] private InventorySO _inventorySO;

    // current active weapon
    private WeaponBase _activeWeapon;

    private bool _isMidSwing;
    private WeaponSO _pendingWeaponSO; // SO которое нужно применить после атаки
    private bool _pendingDeactivate;
    private bool _useControllerInventory;

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
        if (_registerPlayerSingleton)
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("[ActiveWeapon] Multiple components registered as player singleton.");
            Instance = this;
        }

        _weaponMap = new Dictionary<WeaponType, WeaponBase>();
        foreach (var entry in _weaponEntries)
        {
            if (entry.Weapon != null)
                _weaponMap[entry.Type] = entry.Weapon;
        }
        _ownerStats.OnPlayerDeath += OwnerStats_OnPlayerDeath;
    }

    

    private void OnEnable()
    {
        BindInventory();
        if (RunSystem.Instance != null)
            RunSystem.Instance.OnRunStateChanged += HandleRunStateChanged;
    }

    private void OnDisable()
    {
        if (RunSystem.Instance != null)
            RunSystem.Instance.OnRunStateChanged -= HandleRunStateChanged;
        UnbindInventory();
    }

    private void Start()
    {
        _useControllerInventory = _inventorySO == null;
        if (_useControllerInventory && InventoryController.Instance != null)
        {
            UnbindInventory();
            _inventorySO = InventoryController.Instance.GetInventorySO();
            BindInventory();
        }

        foreach (var kv in _weaponMap)
            kv.Value.gameObject.SetActive(false);
        OnInventoryChanged(); // apply the current inventory status at start
    }

    private void Update()
    {
        if (_activeWeapon == null) return;
        ApplyWeaponPivotRotation();
    }

    /// <summary>Used by melee/ranged bots and by <see cref="Ranged"/> for projectile direction.</summary>
    public Vector2 GetAttackAimDirection()
    {
        if (_aimMode == AimMode.MouseRelativeToPlayer)
        {
            if (GameInput.Instance == null || PlayerMovement.Instance == null)
                return Vector2.right;

            Vector3 mousePos = GameInput.Instance.GetMousePosition();
            Vector3 playerScreen = PlayerMovement.Instance.GetPlayerScreenPosition();
            Vector2 delta = mousePos - playerScreen;
            if (delta.sqrMagnitude <= 1e-6f)
                return Vector2.right;
            return delta.normalized;
        }

        if (_worldAimTarget == null)
            return Vector2.right;

        Vector3 dWorld = _worldAimTarget.position - transform.position;
        Vector2 d = new Vector2(dWorld.x, dWorld.y);
        if (d.sqrMagnitude <= 1e-6f)
            return Vector2.right;
        return d.normalized;
    }

    public void SetWorldAimTarget(Transform target)
    {
        _worldAimTarget = target;
        if (_aimMode != AimMode.AimAtWorldTarget)
            _aimMode = AimMode.AimAtWorldTarget;
    }

    public void SetAimMode(AimMode mode) => _aimMode = mode;

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

    private void HandleRunStateChanged(bool _)
    {
        if (!_useControllerInventory) return;
        if (InventoryController.Instance == null) return;

        var nextInventory = InventoryController.Instance.GetInventorySO();
        if (ReferenceEquals(nextInventory, _inventorySO)) return;

        UnbindInventory();
        _inventorySO = nextInventory;
        BindInventory();
        OnInventoryChanged();
    }

    private void BindInventory()
    {
        if (_inventorySO != null)
            _inventorySO.OnInventoryChanged += OnInventoryChanged;
    }

    private void UnbindInventory()
    {
        if (_inventorySO != null)
            _inventorySO.OnInventoryChanged -= OnInventoryChanged;
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
        if (_ownerStats != null)
            _activeWeapon.Owner = _ownerStats;
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

    private void ApplyWeaponPivotRotation()
    {
        if (!RotationEnabled) return;

        Vector2 dir = GetAttackAimDirection();
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OwnerStats_OnPlayerDeath(object sender, System.EventArgs e)
    {
        _activeWeapon.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_registerPlayerSingleton && Instance == this)
            Instance = null;
    }
}
