using System;
using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [SerializeField] private bool _haveTouchDamage = false;

    [Header("Stats")]
    [SerializeField] private EnemySO _enemySO;
    private float _maxHealth;
    private float _currentHealth;
    private float _currentAttack = 1;

    public event EventHandler OnTakeHit;
    public event EventHandler OnDeath;
    private float _nextAttackTime = 0;
    [SerializeField] private float _knockbackMultiplier = 1;
    private Knockback _knockback;
    [SerializeField] private float _knockbackResist;
    private void Awake()
    {
        _knockback = GetComponent<Knockback>();
    }

    private void Start()
    {
        if (_enemySO == null)
            Debug.LogError($"EnemySO is missing on {gameObject.name}");
        if (_knockback == null)
            Debug.LogError($"Knockback script is missing on {gameObject.name}");
        _maxHealth = _enemySO.EnemyHealth;
        _currentHealth = _maxHealth;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (_haveTouchDamage)
        {
            if (Time.time > _nextAttackTime && collision.transform.TryGetComponent(out PlayerStats player))
            {
                player.TakeDamage(_currentAttack, transform, _knockbackMultiplier); //(transform, _currentAttack, ...)
                _nextAttackTime = Time.time + 1f;
            }
        }
    }

    public void TakeDamage(float damage, Transform knockbackSource, float knockbackMultiplier)
    {
        _currentHealth -= damage;
        OnTakeHit?.Invoke(this, EventArgs.Empty);
        DetectDeath();
        _knockback.GetKnockedBack(knockbackSource, knockbackMultiplier, _knockbackResist);
    }

    

    private void DetectDeath()
    {
        if (_currentHealth <= 0)
        {
            Death();
        }
    }

    private void Death()
    {
        OnDeath?.Invoke(this, EventArgs.Empty);
    }
}
