using UnityEngine;

/// <summary>
/// Weapon archetype � determines which WeaponBase component is activated
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

    [Header("AI combat distance")]
    [Tooltip("Maximum effective combat range for ally AI (positioning, target scoring). 0 = weapon-type default.")]
    public float MaxCombatRange;
    [Tooltip("Preferred stand-off distance for ally AI and ML reward shaping. 0 = weapon-type default.")]
    public float OptimalCombatRange;

    public float ResolveMaxCombatRange()
    {
        if (MaxCombatRange > 0f)
            return MaxCombatRange;

        return WeaponType switch
        {
            WeaponType.Sword => MeleeRange > 0f ? MeleeRange : 1.5f,
            WeaponType.Bow => 10f,
            WeaponType.Staff => 8f,
            WeaponType.Talisman => 6f,
            _ => 2f
        };
    }

    public float ResolveOptimalCombatRange()
    {
        if (OptimalCombatRange > 0f)
            return OptimalCombatRange;

        return WeaponType switch
        {
            WeaponType.Sword => MeleeRange > 0f ? MeleeRange * 0.8f : 1.2f,
            WeaponType.Bow => 6f,
            WeaponType.Staff => 5f,
            WeaponType.Talisman => 4f,
            _ => 2f
        };
    }
}
