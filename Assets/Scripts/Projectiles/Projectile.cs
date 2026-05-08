using System;
using System.IO.Abstractions;
using UnityEngine;

[RequireComponent (typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [Header("Base Settings")]
    public float Speed               = 8f;
    public float Lifetime            = 3f;
    public float Damage              = 1f;
    public float Penetrate           = 1f;
    public float KnockbackMultiplier = 1f;
    public float AreaModifier        = 1f;
    public bool  UnlimitedPenetrate  = false;
    public bool  TouchDamage         = true;
    public bool  StaggerApply        = false;
    public bool  CanPenetrateWall    = false;
    public bool  EnemyLaunch         = false;

    [Header("Delayed destroy")]
    [Tooltip("Projectile size, how far it can enter before destroy")]
    public float DelayEntering       = 0.25f;
    [Tooltip("Will be destroyed after it enters the object by 0.25f (DelayEntering) instead instant")]
    public bool  DelayedDestroy      = false;

    public event EventHandler OnProjectileDestroy;

    public Vector2 StartPosition { get; private set; }

    private Vector2 direction;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Destroy(gameObject, Lifetime);
    }

    public void SetDirection(Vector2 dir, Vector3 startPosition)
    {
        direction = dir.normalized;
        StartPosition = startPosition;
        rb.linearVelocity = direction * Speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (EnemyLaunch)
        {
            if (collision.TryGetComponent(out ICharacterEntity target) && target.IsAlive)
            {
                if (TouchDamage) target.TakeDamage(Damage, StartPosition, KnockbackMultiplier);
                PenetrationUpdate();
                return;
            }
        }
        else
        {
            if (collision.TryGetComponent(out EnemyDamage enemy))
            {
                if (TouchDamage) enemy.TakeDamage(Damage, StartPosition, KnockbackMultiplier, StaggerApply);
                PenetrationUpdate();
                return;
            }
        }
        // Разрушаемое окружение
        if (collision.TryGetComponent(out DestructibleEnvironment env))
        {
            if (TouchDamage) env.TakeDamage();
            PenetrationUpdate();
            return;
        }
        // Стена — уничтожаем если нет пробития
        else if (collision.CompareTag("Environment") && !CanPenetrateWall)
            SetDestroy();

    }
    private void PenetrationUpdate()
    {
        if (!UnlimitedPenetrate)
        {
            Penetrate -= 1;
            if (Penetrate <= 0) SetDestroy();
        }
    }

    private void SetDestroy()
    {
        if (DelayedDestroy)
            Destroy(gameObject, DelayEntering/Speed);
        else Destroy(gameObject);
    }

    private void OnDestroy()
    {
        OnProjectileDestroy?.Invoke(this, EventArgs.Empty);
    }
}
