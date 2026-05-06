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
    public float AreaOfEffect = 1f;
    public bool ApplyStagger;


    [Header("Projectile Settings")]
    public float ProjectileSpeed = 8f;
    public float ProjectileLifetime = 3f;
    public float KnockbackMultiplier = 1f;
    public float Penetrate = 1f;

}

