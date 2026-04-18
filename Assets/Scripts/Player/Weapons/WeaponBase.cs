using UnityEngine;

/// <summary>
/// The base class of all weapons. Stores a reference to the owner (ICharacterEntity)
/// and to the WeaponSO with basic parameters. Specific values are applied
/// via ApplyWeaponSO() when equipping.
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    [HideInInspector] public ICharacterEntity Owner;
    [HideInInspector] public WeaponSO WeaponData;

    // Run-time values are copied from WeaponSO + owner's modifiers
    public float DamageMulti { get; protected set; } = 1f;
    public float Cooldown { get; protected set; } = 0.5f;
    public bool RotationEnabled { get; set; } = true;

    protected SpriteRenderer _spriteRenderer;

    protected virtual void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// Applies basic parameters from WeaponSO.
    /// Called from ActiveWeapon after assigning WeaponData.
    /// Can be redefined in subclasses for additional logic.
    /// </summary>
    public virtual void ApplyWeaponSO(WeaponSO data)
    {
        WeaponData = data;
        DamageMulti = data.DamageMultiplier;
        Cooldown = data.AttackCooldown;
        RotationEnabled = data.RotationEnabled;

        if (_spriteRenderer != null && data.WeaponSprite != null)
            _spriteRenderer.sprite = data.WeaponSprite;
    }

    public abstract void Attack();
}
