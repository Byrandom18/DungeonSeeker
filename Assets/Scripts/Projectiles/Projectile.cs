using Google.Protobuf.WellKnownTypes;
using System;
using UnityEngine;

[RequireComponent (typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    public float Speed               = 8f;
    public float Lifetime            = 3f;
    public float Damage              = 1f;
    public float Penetrate           = 1f;
    public float KnockbackMultiplier = 1f;
    public bool  StaggerApply        = false;
    public bool  CanPenetrateWall    = false;
    public bool  EnemyLaunch         = false;

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
                target.TakeDamage(Damage, StartPosition, KnockbackMultiplier);
                PenetrationUpdate();
                return;
            }
        }
        else
        {
            if (collision.TryGetComponent(out EnemyDamage enemy))
            {
                enemy.TakeDamage(Damage, StartPosition, KnockbackMultiplier, StaggerApply);
                PenetrationUpdate();
                return;
            }
        }
        // Разрушаемое окружение
        if (collision.TryGetComponent(out DestructibleEnvironment env))
        {
            env.TakeDamage();
            PenetrationUpdate();
            return;
        }

        // Стена — уничтожаем если нет пробития
        if (collision.CompareTag("Environment") && !CanPenetrateWall)
            Destroy(gameObject);


    }
    private void PenetrationUpdate()
    {
        Penetrate -= 1;
        if (Penetrate <= 0) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        OnProjectileDestroy?.Invoke(this, EventArgs.Empty);
    }
}
