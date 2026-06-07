using System;
using UnityEngine;

[RequireComponent (typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    public float Speed               { get; private set; }
    public float Lifetime            { get; private set; }
    public float Damage              { get; private set; }
    public float Penetrate           { get; private set; }
    public float KnockbackMultiplier { get; private set; }
    public float AreaModifier        { get; private set; }
    public float CritRate            { get; private set; }
    public float CritDamage          { get; private set; }
    public bool EnemyLaunch          { get; private set; }
    public bool StaggerApply         { get; private set; }
    public Vector2 StartPosition     { get; private set; }
    

    [Header("Base Settings")] // edit only in inspector for each projectile variant
    [SerializeField] private bool  _unlimitedPenetrate  = false;
    [SerializeField] private bool  _touchDamage         = true;
    [SerializeField] private bool  _canPenetrateWall    = false;

    [Header("Delayed destroy")]
    [Tooltip("Projectile size, how far it can enter before destroy")]
    [SerializeField] private float _delayEntering       = 0.25f;
    [Tooltip("Will be destroyed after it enters the object by 0.25f (DelayEntering) instead instant")]
    [SerializeField] private bool  _delayedDestroy      = false;

    public event EventHandler OnProjectileDestroy;

    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (EnemyLaunch)
            EnemyProjectileRegistry.Register(this);
    }

    private void OnDisable()
    {
        EnemyProjectileRegistry.Unregister(this);
    }

    public void Configure(ProjectileConfig config)
    {
        Speed =                 config.Speed;
        Lifetime =              config.Lifetime;
        Damage =                config.Damage;
        Penetrate =             config.Penetrate;
        KnockbackMultiplier =   config.KnockbackMultiplier;
        transform.localScale =  Vector2.one * config.Size;
        AreaModifier =          config.AreaModifier;
        CritRate =              config.CritRate;
        CritDamage =            config.CritDamage;

        StaggerApply =          config.StaggerApply;
        EnemyLaunch =           config.EnemyLaunch;

        if (EnemyLaunch)
            EnemyProjectileRegistry.Register(this);

        SetDirection(config.Direction, config.StartPosition);

        Destroy(gameObject, Lifetime);
    }

    private void SetDirection(Vector2 dir, Vector3 startPosition)
    {
        Vector2 direction = dir.normalized;
        StartPosition = startPosition;
        _rb.linearVelocity = direction * Speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (EnemyLaunch)
        {
            if (collision.TryGetComponent(out ICharacterEntity target) && target.IsAlive)
            {
                if (_touchDamage) target.TakeDamage(Damage, StartPosition, KnockbackMultiplier);
                PenetrationUpdate();
                return;
            }
        }
        else
        {
            if (collision.TryGetComponent(out EnemyDamage enemy))
            {
                if (_touchDamage) enemy.TakeDamage(Damage, CritRate, CritDamage, StartPosition, KnockbackMultiplier, StaggerApply);
                PenetrationUpdate();
                return;
            }
        }
        if (collision.TryGetComponent(out DestructibleEnvironment env))
        {
            if (_touchDamage) env.TakeDamage();
            PenetrationUpdate();
            return;
        }
        else if (collision.CompareTag("Environment") && !_canPenetrateWall)
            SetDestroy();

    }
    private void PenetrationUpdate()
    {
        if (!_touchDamage)
        { 
            SetDestroy(); 
            return; 
        }    
        if (!_unlimitedPenetrate)
        {
            Penetrate -= 1;
            if (Penetrate < 0) SetDestroy();
        }
    }

    private void SetDestroy()
    {
        if (_delayedDestroy)
            Destroy(gameObject, _delayEntering/Speed);
        else Destroy(gameObject);
    }

    private void OnDestroy()
    {
        OnProjectileDestroy?.Invoke(this, EventArgs.Empty);
    }
}



[Serializable]
public struct ProjectileConfig
{
    public float Speed;
    public float Lifetime;
    public float Damage;
    public float Penetrate;
    public float KnockbackMultiplier;
    public float Size;         // for self scale
    public float AreaModifier; // for children objects scale (explosion, etc)
    public float CritRate;
    public float CritDamage;

    public bool StaggerApply;
    public bool EnemyLaunch;

    public Vector2 Direction;
    public Vector3 StartPosition;

    public ProjectileConfig(float speed = 8f, float lifetime = 5f, float damage = 1f, float size = 1f, float areaModifier = 1f)
    {
        Speed = speed;
        Lifetime = lifetime;
        Damage = damage;
        CritRate = 0;
        CritDamage = 0;

        Penetrate = 0;
        KnockbackMultiplier = 1f;
        Size = size;                      // for self scale
        AreaModifier = areaModifier;      // for children objects scale (explosion, etc)

        StaggerApply = true;
        EnemyLaunch = false;

        Direction = Vector2.right;
        StartPosition = Vector3.zero;
    }
}