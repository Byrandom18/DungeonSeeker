using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyDamage : MonoBehaviour
{
    [SerializeField] private EnemySO _enemySO;
    [SerializeField] private bool    _haveTouchDamage = false;

    [Header("Stagger settings")]
    [SerializeField] private bool  _staggerImmune   = false;
    [SerializeField] private float _staggerDuration = 1f;

    [SerializeField] private Slider _healthBar;

    public bool InStagger         = false;
    public bool CanReceiveStagger = true;
    public bool IsAlive           = true;
    public bool IsChasing         = false;

    public float KnockbackMultiplier = 1f;

    // Runtime stats
    private float _currentHealth;
    private float _baseAttack;
    public  float CurrentAttack;

    private float _knockbackResist;
    private float _staggerEndTime;
    private float _nextAttackTime;

    private Knockback         _knockback;
    private CapsuleCollider2D _hitBox;
    private BoxCollider2D     _collisionBox;

    public event EventHandler OnTakeHit;
    public event EventHandler OnDeath;

    private void Awake()
    {
        _knockback = GetComponent<Knockback>();
        _hitBox = GetComponent<CapsuleCollider2D>();
        _collisionBox = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        if (_enemySO == null) Debug.LogError($"EnemySO missing on {gameObject.name}");
        if (_knockback == null) Debug.LogError($"Knockback missing on {gameObject.name}");
        InitializeStats();
        InitializeHealthBar();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!_haveTouchDamage) return;
        if (Time.time <= _nextAttackTime) return;

        if (collision.TryGetComponent(out ICharacterEntity target) && target.IsAlive)
        {
            target.TakeDamage(CurrentAttack, transform.position, KnockbackMultiplier);
            _nextAttackTime = Time.time + 1f;
            _knockback.GetKnockedBack(collision.transform.position, 1f, _knockbackResist);
        }
    }

    public void TakeDamage(
        float damage,
        Vector3 knockbackSource,
        float knockbackMultiplier,
        bool isStaggeringAttack)
    {
        if (!IsAlive) return;

        _currentHealth -= damage;
        UpdateHealthBar();
        if (isStaggeringAttack) ApplyStagger();

        IsChasing = true;
        OnTakeHit?.Invoke(this, EventArgs.Empty);
        _knockback.GetKnockedBack(knockbackSource, knockbackMultiplier, _knockbackResist);
        DetectDeath();
    }

    public EnemySO GetEnemySO()
    {
        return _enemySO;
    }

    private void ApplyStagger()
    {
        if (!_staggerImmune && CanReceiveStagger)
            StartCoroutine(StaggerCoroutine());
    }

    private IEnumerator StaggerCoroutine()
    {
        InStagger = true;
        _staggerEndTime = Time.time + _staggerDuration;
        yield return new WaitForSeconds(_staggerDuration);
        if (Time.time >= _staggerEndTime) InStagger = false;
    }

    private void InitializeStats()
    {
        _currentHealth = _enemySO.EnemyMaxHealth;

        _baseAttack   = _enemySO.EnemyBaseAttack;
        CurrentAttack = _baseAttack;

        _staggerImmune   = _enemySO.StaggerImmune;
        _staggerDuration = _enemySO.StaggerDuration;

        KnockbackMultiplier = _enemySO.KnockbackMultiplier;
        _knockbackResist    = _enemySO.KnockbackResist;

        _knockback.KnockbackForce            = _enemySO.KnockbackSelfForce;
        //_knockback.KnockbackMovingTimerMax   = _enemySO.KnockbackDuration;

        _haveTouchDamage = _enemySO.EnableTouchDamage;
    }

    private void InitializeHealthBar()
    {
        if (_healthBar == null)
        {
            Debug.LogError("Healthbar not assigned on " + gameObject.name);
            return;
        }
        _healthBar.maxValue = _enemySO.EnemyMaxHealth;
        _healthBar.value = _currentHealth;
        _healthBar.gameObject.SetActive(false);
    }

    private void UpdateHealthBar()
    {
        if (_healthBar == null) return;
        _healthBar.gameObject.SetActive(true);
        _healthBar.value = _currentHealth;
    }

    private void DetectDeath()
    {
        if (_currentHealth <= 0f) Death();
    }

    private void Death()
    {
        _hitBox.enabled = false;
        _collisionBox.enabled = false;
        _healthBar.gameObject.SetActive(false);
        IsAlive = false;
        OnDeath?.Invoke(this, EventArgs.Empty);
    }
}
