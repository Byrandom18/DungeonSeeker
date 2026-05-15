using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the character's active weapon (one instance per character).
/// </summary>
public class ActiveWeapon : MonoBehaviour
{
    public enum AimMode
    {
        MouseRelativeToPlayer,
        AimAtWorldTarget
    }

    [Header("Aiming")]
    [SerializeField] private AimMode _aimMode = AimMode.MouseRelativeToPlayer;
    [SerializeField] private Transform _worldAimTarget;
    [SerializeField] private PlayerMovement _aimMovementReference;

    [Header("Owner")]
    [SerializeField] private PlayerStats _ownerStats;

    [Header("Weapon components (by 1 on each WeaponType)")]
    [SerializeField] private WeaponEntry[] _weaponEntries;

    [Header("Inventory")]
    [SerializeField] private EquipmentComponent _equipment;
    [SerializeField] private InventorySO _inventorySO;

    private WeaponBase _activeWeapon;
    private bool _isMidSwing;
    private WeaponSO _pendingWeaponSO;
    private bool _pendingDeactivate;
    private bool _useSharedInventory;
    private string _ownerId;

    private Dictionary<WeaponType, WeaponBase> _weaponMap;

    public bool RotationEnabled
    {
        get => _activeWeapon != null && _activeWeapon.RotationEnabled;
        set
        {
            if (_activeWeapon != null)
                _activeWeapon.RotationEnabled = value;
        }
    }

    [System.Serializable]
    public struct WeaponEntry
    {
        public WeaponType Type;
        public WeaponBase Weapon;
    }

    private void Awake()
    {
        if (_ownerStats == null)
            _ownerStats = GetComponent<PlayerStats>();
        if (_equipment == null)
            _equipment = GetComponent<EquipmentComponent>();
        if (_aimMovementReference == null)
            _aimMovementReference = GetComponent<PlayerMovement>();

        _weaponMap = new Dictionary<WeaponType, WeaponBase>();
        foreach (var entry in _weaponEntries)
        {
            if (entry.Weapon != null)
                _weaponMap[entry.Type] = entry.Weapon;
        }

        if (_ownerStats != null)
            _ownerStats.OnPlayerDeath += OwnerStats_OnPlayerDeath;

        _ownerId = _equipment != null ? _equipment.PartyMemberId : null;
        _useSharedInventory = _inventorySO == null;
    }

    private void OnEnable()
    {
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
        ResolveInventoryReference();

        if (_equipment != null)
            _ownerId = _equipment.PartyMemberId;

        foreach (var kv in _weaponMap)
            kv.Value.gameObject.SetActive(false);

        OnInventoryChanged();
    }

    private void Update()
    {
        if (_activeWeapon == null) return;
        ApplyWeaponPivotRotation();
    }

    public Vector2 GetAttackAimDirection()
    {
        if (_aimMode == AimMode.MouseRelativeToPlayer)
        {
            if (GameInput.Instance == null)
                return Vector2.right;

            PlayerMovement moveRef = _aimMovementReference;
            if (moveRef == null && PartyManager.Instance != null)
                moveRef = PartyManager.Instance.LeaderMovement;

            if (moveRef == null)
                return Vector2.right;

            Vector3 mousePos = GameInput.Instance.GetMousePosition();
            Vector3 screenPos = moveRef.GetPlayerScreenPosition();
            Vector2 delta = mousePos - screenPos;
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

    private void ResolveInventoryReference()
    {
        UnbindInventory();

        if (_useSharedInventory && InventoryController.Instance != null)
            _inventorySO = InventoryController.Instance.GetInventorySO();

        BindInventory();
    }

    private void OnInventoryChanged()
    {
        if (_inventorySO == null) return;
        if (string.IsNullOrEmpty(_ownerId) && _equipment != null)
            _ownerId = _equipment.PartyMemberId;

        WeaponSO equippedSO = null;
        foreach (var data in _inventorySO.Items)
        {
            if (!data.IsEquipped) continue;
            if (!string.IsNullOrEmpty(_ownerId) && data.EquippedOwnerId != _ownerId) continue;
            if (data.Item == null || data.Item.EquipmentSlot != EquipmentSlot.Weapon) continue;
            if (data.Item.WeaponSO == null) continue;

            equippedSO = data.Item.WeaponSO;
            break;
        }

        if (_isMidSwing)
        {
            _pendingWeaponSO = equippedSO;
            _pendingDeactivate = equippedSO == null;
            return;
        }

        if (equippedSO != null) ActivateWeapon(equippedSO);
        else DeactivateAll();
    }

    private void HandleRunStateChanged(bool _)
    {
        ResolveInventoryReference();
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

    private void OwnerStats_OnPlayerDeath(object sender, System.EventArgs e) => DeactivateAll();

    private void OnDestroy()
    {
        if (_ownerStats != null)
            _ownerStats.OnPlayerDeath -= OwnerStats_OnPlayerDeath;
    }
}
