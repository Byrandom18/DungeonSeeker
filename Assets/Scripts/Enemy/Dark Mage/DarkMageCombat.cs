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
        // Fallback
        return PlayerMovement.Instance != null
            ? PlayerMovement.Instance.transform.position
            : transform.position + Vector3.right;
    }

    private void SpawnProjectile(Vector2 direction, float damage)
    {
        Vector3 spawnPos = transform.position + new Vector3(0f, 0.5f);
        GameObject go = Instantiate(_projectilePrefab, spawnPos, Quaternion.identity);
        go.transform.localScale = Vector3.one * _sizeMultiplier;

        if (go.TryGetComponent(out Projectile p))
        {
            p.Speed = _baseProjectileSpeed;
            p.Lifetime = _baseProjectileLifetime;
            p.Damage = _damageMultiplier * damage;
            p.EnemyLaunch = true;
            p.SetDirection(direction, transform.position);
        }
    }
}
