using UnityEngine;

public class AreaAttack : MonoBehaviour
{
    [SerializeField] private Projectile _projectile;
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private LayerMask _characterLayer;
    [SerializeField] private LayerMask _environmentLayer;
    [SerializeField] private GameObject _deathVFXPrefab;

    public float AreaMulti = 1;
    public float Radius = 0.5f;
    public bool IsEnemyLaunch;
    public bool StaggerApply = false;
    public float KnockbackMultiplier = 1;
    public float Damage = 1;
    public Vector3 SourcePosition;

    private void Start()
    {
        if (_projectile != null)
        {
            _projectile.OnProjectileDestroy += Projectile_OnProjectileDestroy;
            AreaMulti = _projectile.AreaModifier;
        }
        else Activate();
    }

    private void Activate()
    {
        if (IsEnemyLaunch)
        {
            CheckCharactersInCircle();
        }
        else Destroy(gameObject);
    }

    private void Projectile_OnProjectileDestroy(object sender, System.EventArgs e)
    {
        CheckEnemiesInCircle();
    }

    private void CheckEnemiesInCircle()
    {
        float radius = Radius * AreaMulti;
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, radius, _enemyLayer);
        Collider2D[] hitEnvironment = Physics2D.OverlapCircleAll(transform.position, radius, _environmentLayer);
        ShowDeathVFX();
        foreach (Collider2D hitCollider in hitEnemies)
        {
            if (hitCollider.transform.TryGetComponent(out EnemyDamage enemy))
            {
                if (_projectile != null)
                {
                    float damage = _projectile.Damage;
                    Vector3 sourcePosition = _projectile.StartPosition;
                    float knockbackMulti = _projectile.KnockbackMultiplier;
                    bool isStagger = _projectile.StaggerApply;
                    enemy.TakeDamage(damage, sourcePosition, knockbackMulti, isStagger);
                }
                else
                {
                    float damage = Damage;
                    Vector3 sourcePosition = SourcePosition;
                    float knockbackMulti = KnockbackMultiplier;
                    bool isStagger = StaggerApply;
                    enemy.TakeDamage(damage, sourcePosition, knockbackMulti, isStagger);
                }
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

    private void CheckCharactersInCircle()
    {
        float radius = Radius * AreaMulti;
        Collider2D[] hitCharacters = Physics2D.OverlapCircleAll(transform.position, radius, _characterLayer);
        Collider2D[] hitEnvironment = Physics2D.OverlapCircleAll(transform.position, radius, _environmentLayer);
        ShowDeathVFX();
        foreach (Collider2D hitCollider in hitCharacters)
        {
            if (hitCollider.transform.TryGetComponent(out ICharacterEntity character))
            {
                if (_projectile != null)
                {
                    float damage = _projectile.Damage;
                    Vector3 sourcePosition = _projectile.StartPosition;
                    float knockbackMulti = _projectile.KnockbackMultiplier;
                    character.TakeDamage(damage, sourcePosition, knockbackMulti);
                }
                else
                {
                    float damage = Damage;
                    Vector3 sourcePosition = SourcePosition;
                    float knockbackMulti = KnockbackMultiplier;
                    character.TakeDamage(damage, sourcePosition, knockbackMulti);
                }
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
        if (_projectile != null)
        {
            _projectile.OnProjectileDestroy -= Projectile_OnProjectileDestroy;
        }
    }
}
