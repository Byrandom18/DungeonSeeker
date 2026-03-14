using UnityEngine;
using System;

public class Ranged : WeaponBase
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private float _baseProjectileSpeed = 8;
    [SerializeField] private float _baseProjectileLifetime = 3;
    [SerializeField] private float _sizeMultiplier = 1;

    public event EventHandler OnRangedAttack;

    public override void Attack()
    {
        OnRangedAttack?.Invoke(this, EventArgs.Empty);
        Vector3 mousePos = GameInput.Instance.GetMousePosition();
        Vector3 playerPosition = PlayerMovement.Instance.GetPlayerScreenPosition();
        Vector3 direction = mousePos - playerPosition;
        float attack = PlayerStats.Instance.CurrentAtk;
        SpawnProjectile(direction, attack);
    }

    private void SpawnProjectile(Vector2 direction, float damage)
    {
        Vector3 spawnPosition = transform.position; 
        GameObject projectile = Instantiate(_projectilePrefab, spawnPosition, Quaternion.identity);

        projectile.transform.localScale = Vector3.one * _sizeMultiplier;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        if (projectile.TryGetComponent(out Projectile projectileScript))
        {
            projectileScript.Speed = _baseProjectileSpeed;
            projectileScript.Lifetime = _baseProjectileLifetime;
            projectileScript.Damage = DamageMulti * damage;
            projectileScript.EnemyLaunch = false;
            projectileScript.SetDirection(direction, transform.position);
        }
    }
}
