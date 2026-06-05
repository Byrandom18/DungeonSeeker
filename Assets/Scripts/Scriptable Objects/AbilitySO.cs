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

    [Header("AI")]
    public AbilityBehaviorTag BehaviorTag = AbilityBehaviorTag.SingleTarget;
    public AbilityTargetType TargetType = AbilityTargetType.Enemy;
    public float OptimalRange = 8f;
    public float MaxRange = 12f;
    public float MinRange;
    public int MinEnemiesForAoE = 2;
    [Range(0f, 1f)] public float AllyHealThreshold = 0.5f;
    [Range(0f, 1f)] public float SelfDefenceThreshold = 0.3f;

    [Header("Support Settings")]
    public float HealAmount = 30f;
    public float ShieldAmount = 40f;
    public float ShieldDuration = 4f;
    public float ZoneTickInterval = 0.5f;
    public float ZoneDuration = 2f;

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

public enum AbilityTargetType
{
    Enemy,
    Ally,
    Self,
    Ground,
    Direction
}