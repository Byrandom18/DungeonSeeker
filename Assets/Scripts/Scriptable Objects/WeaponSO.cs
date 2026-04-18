using UnityEngine;

/// <summary>
/// Weapon archetype — determines which WeaponBase component is activated
/// and which animation/visual logic ActiveWeapon uses.
/// </summary>
public enum WeaponType
{
    Sword,
    Bow,
    Staff,
    Talisman
}

/// <summary>
/// ScriptableObject with the basic characteristics of a specific weapon.
/// Is assigned to the ItemSO.WeaponSO for items of the Equipment / Weapon type.
///
/// When equipped, ActiveWeapon reads this SO and applies
/// all values to the desired WeaponBase component.
/// </summary>
[CreateAssetMenu(fileName = "WeaponSO", menuName = "Scriptable Objects/WeaponSO")]
public class WeaponSO : ScriptableObject
{
    [Header("Identity")]
    public WeaponType WeaponType;
    public Sprite WeaponSprite;
    public string DisplayName;

    [Header("Base combat stats")]
    public float DamageMultiplier = 1f;
    public float AttackCooldown = 0.5f;
    public bool RotationEnabled = true;
    public bool LockRotationOnSwing = false;

    [Header("Projectile settings (Ranged / Staff / Talisman)")]
    public GameObject ProjectilePrefab;
    public float ProjectileSpeed = 8f;
    public float ProjectileLifetime = 3f;
    public float SizeMultiplier = 1f;
    public int ProjectileCount = 1;
    public float SpreadAngle = 15f;
    public float Penetrate = 1f;
    public bool ApplyStagger = false;
    public float KnockbackMultiplier = 1f;

    [Header("Melee settings (Sword / Dagger)")]
    public float MeleeRange = 1f;
}
