using UnityEngine;

[RequireComponent (typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    public float Speed = 8f;
    public float Lifetime = 3f;
    public float Damage = 1;
    public float Penetrate = 1;
    public float KnockbackMultiplier = 1;
    public bool StaggerApply = false;
    public bool CanPenetrateWall = false;
    //public float defShred = 0;
    
    public bool EnemyLaunch = false;

    private Transform _startPosition;
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

    public void SetDirection(Vector2 dir, Transform startPosition)
    {
        direction = dir.normalized;
        _startPosition = startPosition;
        rb.linearVelocity = direction * Speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (EnemyLaunch && collision.CompareTag("Player") && collision.transform.TryGetComponent(out PlayerStats player))
        {
            player.TakeDamage(Damage, _startPosition, KnockbackMultiplier);
            PenetrationUpdate();
        }
        else if (!EnemyLaunch && collision.CompareTag("Enemy") && collision.transform.TryGetComponent(out EnemyDamage enemy))
        {
            enemy.TakeDamage(Damage, _startPosition, KnockbackMultiplier, StaggerApply);
            PenetrationUpdate();
        }
        else if (collision.CompareTag("Environment") && collision.TryGetComponent(out DestructibleEnvironment environment))
        {
            environment.TakeDamage();
            PenetrationUpdate();
        }
        else if (collision.CompareTag("Environment") && !CanPenetrateWall) Destroy(gameObject);

        
    }
    private void PenetrationUpdate()
    {
        Penetrate -= 1;
        if (Penetrate <= 0) Destroy(gameObject);
    }
}
