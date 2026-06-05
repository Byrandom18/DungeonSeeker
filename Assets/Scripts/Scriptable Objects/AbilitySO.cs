using UnityEngine;

[CreateAssetMenu(fileName = "AbilitySO", menuName = "Scriptable Objects/AbilitySO")]
public class AbilitySO : ScriptableObject
{
    public string DisplayName;
    public Sprite Icon;
    [TextArea] public string Description;

    public AbilityType AbilityType;
    public WeaponType RequiredWeapon;
    public bool RequiresWeapon;

    public float Cooldown;
    public float ManaCost;
    public GameObject EffectPrefab;

    [Header("Stats")]
    public float DamageMultiplier = 1f;
    public float BaseScale = 1f;
    public float AreaOfEffect = 1f;
    public float KnockbackMultiplier = 1f;
    public bool StaggerApply;

    [Header("Projectile Settings")]
    public float ProjectileSpeed = 8f;
    public float ProjectileLifetime = 3f;
    public float Penetrate = 1f;
    public bool UnlimitedPenetrate = false;
    public bool CanPenetrateWall = false;
    public bool DelayedDestroy = false;
    public float DelayEntering = 0.25f;
}

public enum AbilityBehaviorTag
{
    SingleTarget,
    MultiTarget,
    Defence,
    Support,
    Heal
}