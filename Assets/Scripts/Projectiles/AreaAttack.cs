using Google.Protobuf.WellKnownTypes;
using UnityEngine;

public class AreaAttack : MonoBehaviour
{
    [SerializeField] private Projectile _projectile;
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private LayerMask _characterLayer;
    [SerializeField] private LayerMask _environmentLayer;
    [SerializeField] private GameObject _deathVFXPrefab;

    [Tooltip("set in inspector if parent is projectile")]
    [SerializeField] private float _radius = 0.5f;

    private float _areaMulti = 1;
    private bool _enemyLaunch;
    private bool _staggerApply = false;
    private float _knockbackMultiplier = 1;
    private float _damage = 1;
    private Vector3 _damageSource;

    private void Start()
    {
        if (_projectile != null)
        {
            _projectile.OnProjectileDestroy += Projectile_OnProjectileDestroy;

            _areaMulti = _projectile.AreaModifier;
            _enemyLaunch = _projectile.EnemyLaunch;
            _damage = _projectile.Damage;
            _damageSource = _projectile.StartPosition;
            _knockbackMultiplier = _projectile.KnockbackMultiplier;
            _staggerApply = _projectile.StaggerApply;
        }
    }

    public void Configure(// TODO: constructor + crits
        float damage,
        float baseRadius,
        float areaMuliplier,
        bool enemyLaunch,
        bool staggerApply,
        float knockbackMultiplier,
        Vector3 damageSource)
    {
        _damage = damage;
        _radius = baseRadius;
        transform.localScale = Vector2.one * areaMuliplier;
        _enemyLaunch = enemyLaunch;
        _staggerApply = staggerApply;
        _knockbackMultiplier = knockbackMultiplier;
        _damageSource = damageSource;

        CheckEntitiesInCircle();
    }

    private void Projectile_OnProjectileDestroy(object sender, System.EventArgs e)
    {
        CheckEntitiesInCircle();
    }

    private void CheckEntitiesInCircle()
    {
        float radius = _radius * _areaMulti;
        ShowDeathVFX();

        if (!_enemyLaunch)
        {
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, radius, _enemyLayer);
            foreach (Collider2D hitCollider in hitEnemies)
            {
                if (!hitCollider.isTrigger) continue;
                if (hitCollider.transform.TryGetComponent(out EnemyDamage enemy))
                    enemy.TakeDamage(_damage, 0, 0, _damageSource, _knockbackMultiplier, _staggerApply);
            } //                          TODO: crits
        }
        else
        {
            Collider2D[] hitCharacters = Physics2D.OverlapCircleAll(transform.position, radius, _characterLayer);
            foreach (Collider2D hitCollider in hitCharacters)
            {
                if (!hitCollider.isTrigger) continue;
                if (hitCollider.transform.TryGetComponent(out ICharacterEntity character))
                    character.TakeDamage(_damage, _damageSource, _knockbackMultiplier);
            }
        }

        Collider2D[] hitEnvironment = Physics2D.OverlapCircleAll(transform.position, radius, _environmentLayer);
        foreach (Collider2D hitCollider in hitEnvironment)
        {
            if (hitCollider.TryGetComponent(out DestructibleEnvironment environment))
                environment.TakeDamage();
        }

    }


    private void ShowDeathVFX()
    {
        GameObject go = Instantiate(_deathVFXPrefab, transform.position, Quaternion.identity);
        go.transform.localScale = Vector3.one * _areaMulti;
    }

    private void OnDestroy()
    {
        if (_projectile != null)
        {
            _projectile.OnProjectileDestroy -= Projectile_OnProjectileDestroy;
        }
    }
}
