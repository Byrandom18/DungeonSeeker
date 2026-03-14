using UnityEngine;

public class AllyExplosion : MonoBehaviour
{
    [SerializeField] private Projectile _projectile;
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private LayerMask _environmentLayer;
    [SerializeField] private GameObject _deathVFXPrefab;

    [SerializeField] private float _areaMulti = 1;
    [SerializeField] private float _radius = 0.5f;


    private void Start()
    {
        _projectile.OnProjectileDestroy += Projectile_OnProjectileDestroy;
        _areaMulti = _projectile.transform.localScale.x;


    }

    private void Projectile_OnProjectileDestroy(object sender, System.EventArgs e)
    {
        CheckEnemiesInCircle();
    }

    private void CheckEnemiesInCircle()
    {
        float radius = _radius * _areaMulti;
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, radius, _enemyLayer);
        Collider2D[] hitEnvironment = Physics2D.OverlapCircleAll(transform.position, radius, _environmentLayer);
        ShowDeathVFX();
        foreach (Collider2D hitCollider in hitEnemies)
        {
            if (hitCollider.transform.TryGetComponent(out EnemyDamage enemy))
            {
                float damage = _projectile.Damage;
                Vector3 sourcePosition = _projectile.StartPosition;
                float knockbackMulti = _projectile.KnockbackMultiplier;
                bool isStagger = _projectile.StaggerApply;
                enemy.TakeDamage(damage, sourcePosition, knockbackMulti, isStagger);
            }
        }
        foreach (Collider2D hitCollider in hitEnvironment)
        {
            if (hitCollider.TryGetComponent(out DestructibleEnvironment environment))
            {
                environment.TakeDamage();
            }
        }

    }

    private void ShowDeathVFX()
    {
        Instantiate(_deathVFXPrefab, transform.position, Quaternion.identity);
    }

    private void OnDestroy()
    {
        _projectile.OnProjectileDestroy -= Projectile_OnProjectileDestroy;
    }
}
