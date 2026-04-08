using System;
using UnityEngine;


public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;
    //public static event System.Action<Transform> OnPlayerSpawned;
    private Knockback _knockback;
    public event EventHandler OnPlayerDeath;

    //[Header("UI Elements")]
    //private Slider _healthBar;
    //private Text _levelText;
    //private Text _goldText;
    //private Text _gemsText;
    //public ExpBar expBar;

    [Header("Combat Stats")]
    public float Health = 100;
    public float BaseHealth = 100;
    public float HealthMod;
    public float MaxHealth;
    public float HealthRegen;
    public float BaseAtk = 10;
    public float CurrentAtk = 1;
    public float AtkMod;
    public float DamageMod;
    public float Luck;
    public float CritRate = 5;
    public float CritDamage = 50;
    public float Def;
    public float SpeedMod;
    public int PenetrationBoost;
    public float ProjectileSpeed;
    public float Durations;
    public float CdRed;
    public int AddProjectile;
    public float AreaMod;
    public float DefShred;
    public bool Invulnerability = false;
    public float KnockbackResist;
    public float KnockbackMultiplier = 1;

    public bool IsAlive = true;
    public event EventHandler OnFlashBlink;
    private BoxCollider2D _collisionCollider2D;
    private CircleCollider2D _hitboxCollider2D;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            Debug.Log("PlayerStats can not assign the Instance on " + gameObject.name);
            return;
        }
        _knockback = GetComponent<Knockback>();
        _collisionCollider2D = GetComponent<BoxCollider2D>();
        _hitboxCollider2D = GetComponent<CircleCollider2D>();
    }

    private void Start()
    {
        InitializeStats();
    }


    public void TakeDamage(float damage, Vector3 knockbackSource, float knockbackMultiplier)
    {
        if (!IsAlive || Invulnerability) return;

        damage -= Def;
        //damage = Mathf.Max(1f, damage * (1 - def / (def + 100f)));

        Health -= damage;
        Health = Mathf.Max(0, Health);
        OnFlashBlink?.Invoke(this, EventArgs.Empty);
        Debug.Log("Player Health = " + Health);

        if (Health <= 0 && IsAlive)
        {
            Die();
        }
        if (IsAlive) _knockback.GetKnockedBack(knockbackSource, knockbackMultiplier, KnockbackResist);
    }

    

    private void InitializeStats()
    {
        CurrentAtk = BaseAtk * (1 + AtkMod / 100);
        MaxHealth = BaseHealth * (1 + HealthMod / 100);
        Health = MaxHealth;
    }
    

    private void Die()
    {
        IsAlive = false;
        OnPlayerDeath?.Invoke(this, EventArgs.Empty);

        _hitboxCollider2D.enabled = false;
        _collisionCollider2D.enabled = false;

    }
}
