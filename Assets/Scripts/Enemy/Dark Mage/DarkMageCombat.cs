using UnityEngine;

public class DarkMageCombat : MonoBehaviour
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private float _damageMultiplier = 1;
    [SerializeField] private float _baseProjectileSpeed = 8;
    [SerializeField] private float _baseProjectileLifetime = 3;
    [SerializeField] private float _sizeMultiplier = 1;

    public void Shoot(float damage)
    {
        Vector2 direction = (PlayerMovement.Instance.transform.position - transform.position).normalized;
        SpawnProjectile(direction, damage);
    }

    private void SpawnProjectile(Vector2 direction, float damage)
    {
        Vector3 spawnPosition = transform.position + new Vector3(0, 0.5f);
        GameObject projectile = Instantiate(_projectilePrefab, spawnPosition, Quaternion.identity);
        projectile.transform.localScale = Vector3.one * _sizeMultiplier;
        if (projectile.TryGetComponent(out Projectile projectileScript))
        {
            projectileScript.Speed = _baseProjectileSpeed;
            projectileScript.Lifetime = _baseProjectileLifetime;
            projectileScript.Damage = _damageMultiplier * damage;
            projectileScript.EnemyLaunch = true;
            projectileScript.SetDirection(direction, transform.position);
        }


    }
}
