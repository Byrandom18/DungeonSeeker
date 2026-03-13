using System;
using System.Collections;
using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [SerializeField] private bool _haveTouchDamage = false;

    [Header("Stats")]
    [SerializeField] private EnemySO _enemySO;
    private float _baseHealth;
    private float _maxHealth;
    private float _currentHealth;
    private float _baseAttack = 1;
    public float CurrentAttack = 1;

    [Header("Stagger settings")]
    [SerializeField] private bool _staggerImmune = false;
    public bool InStagger = false;
    public bool CanReceiveStagger = true;
    [SerializeField] private float _staggerDuration = 1f;
    private float _staggerEndTime = 0f;

    public event EventHandler OnTakeHit;
    public event EventHandler OnDeath;
    private float _nextAttackTime = 0;
    public float KnockbackMultiplier = 1;
    private Knockback _knockback;
    [SerializeField] private float _knockbackResist;
    public bool IsAlive = true;
    private CapsuleCollider2D _hitBox;

    private void Awake()
    {
        _knockback = GetComponent<Knockback>();
        _hitBox = GetComponent<CapsuleCollider2D>();
    }

    private void Start()
    {
        if (_enemySO == null)
            Debug.LogError($"EnemySO is missing on {gameObject.name}");
        if (_knockback == null)
            Debug.LogError($"Knockback script is missing on {gameObject.name}");
        InitializeStats();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (_haveTouchDamage)
        {
            if (Time.time > _nextAttackTime && collision.transform.TryGetComponent(out PlayerStats player))
            {
                player.TakeDamage(CurrentAttack, transform.position, KnockbackMultiplier); //(transform, _currentAttack, ...)
                _nextAttackTime = Time.time + 1f;
                _knockback.GetKnockedBack(collision.transform.position, 1f, _knockbackResist);
            }
        }
    }

    public void TakeDamage(float damage, 
        Vector3 knockbackSource, float knockbackMultiplier,
        bool isStaggeringAttack)
    {
        if (IsAlive)
        {
            _currentHealth -= damage;
            if (isStaggeringAttack) ApplyStagger();
            OnTakeHit?.Invoke(this, EventArgs.Empty);
            DetectDeath();
            _knockback.GetKnockedBack(knockbackSource, knockbackMultiplier, _knockbackResist);
        }
    }

    private void InitializeStats()
    {
        _baseHealth = _enemySO.EnemyMaxHealth;
        _maxHealth = _baseHealth; //* _healthModifier ...
        _currentHealth = _maxHealth;

        _baseAttack = _enemySO.EnemyBaseAttack;
        CurrentAttack = _baseAttack; //* _attackModifier ...

        _staggerImmune = _enemySO.StaggerImmune;
        _staggerDuration = _enemySO.StaggerDuration;

        KnockbackMultiplier = _enemySO.KnockbackMultiplier;
        _knockbackResist = _enemySO.KnockbackResist;
        _knockback.KnockbackForce = _enemySO.KnockbackSelfForce;
        _knockback.KnockbackMovingTimerMax = _enemySO.KnockbackDuration;

        _haveTouchDamage = _enemySO.EnableTouchDamage;
    }

    private void ApplyStagger()
    {
        if (!_staggerImmune && CanReceiveStagger)
        {
            
            StartCoroutine(StaggerCoroutine());
        }
    }

    private IEnumerator StaggerCoroutine()
    {
        InStagger = true;
        _staggerEndTime = Time.time + _staggerDuration;
        yield return new WaitForSeconds(_staggerDuration);
        if (Time.time >= _staggerEndTime) InStagger = false;
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
        _hitBox.enabled = false;
        IsAlive = false;
        OnDeath?.Invoke(this, EventArgs.Empty);
    }
}
