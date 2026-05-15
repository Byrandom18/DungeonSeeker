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
        Vector3 targetPos = GetTargetPosition();
        Vector2 direction = (targetPos - transform.position).normalized;
        SpawnProjectile(direction, damage);
    }

    private Vector3 GetTargetPosition()
    {
        if (PartyManager.Instance != null)
        {
            var target = PartyManager.Instance.GetNearestTarget(transform.position);
            if (target != null) return target.Transform.position;
        }
        if (PartyManager.Instance != null && PartyManager.Instance.LeaderTransform != null)
            return PartyManager.Instance.LeaderTransform.position;

        return transform.position + Vector3.right;
    }

    private void SpawnProjectile(Vector2 direction, float damage)
    {
        Vector3 spawnPos = transform.position + new Vector3(0f, 0.5f);
        GameObject go = Instantiate(_projectilePrefab, spawnPos, Quaternion.identity);
        go.transform.localScale = Vector3.one * _sizeMultiplier;

        if (go.TryGetComponent(out Projectile p))
        {
            var config = new ProjectileConfig
            {
                Speed = _baseProjectileSpeed,
                Lifetime = _baseProjectileLifetime,
                Damage = _damageMultiplier * damage,
                Size = _sizeMultiplier,
                AreaModifier = _sizeMultiplier,
                EnemyLaunch = true,
                Direction = direction,
                StartPosition = transform.position
            };

            p.Configure(config);
        }
    }
}
