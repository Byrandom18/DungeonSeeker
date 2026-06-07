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

    public bool InStagger         { get; private set; } = false;
    public bool CanReceiveStagger { get; private set; } = true;
    public bool IsAlive           { get; private set; } = true;
    public bool InCombat          { get; private set; } = false;
    public float CurrentAttack    { get; private set; }
    public float CurrentHealth    { get; private set; }
    public float MaxHealth        => _enemySO != null ? _enemySO.EnemyMaxHealth : 1f;
    public float HealthPercent    => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;

    public float KnockbackMultiplier = 1f;

    // Runtime stats
    private float _baseAttack;

    private float _knockbackResist;
    private float _staggerEndTime;
    private float _nextAttackTime;

    private Knockback         _knockback;
    private CapsuleCollider2D _hitBox;
    private BoxCollider2D     _collisionBox;

    public event EventHandler OnTakeHit;
    public event EventHandler OnDeath;

    public ICharacterEntity LastAttacker { get; private set; }

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
        float critRate,
        float critDamage,
        Vector3 knockbackSource,
        float knockbackMultiplier,
        bool isStaggeringAttack)
    {
        if (!IsAlive) return;
        float critRoll = UnityEngine.Random.Range(0f, 100f);
        if (critRoll <= critRate)
        {
            damage *= 1 + critDamage / 100;
        }
        Debug.Log(gameObject.name + " receive " + damage);
        CurrentHealth -= damage;
        UpdateHealthBar();
        if (isStaggeringAttack) ApplyStagger();

        InCombat = true;
        LastAttacker = ResolveAttacker(knockbackSource);
        OnTakeHit?.Invoke(this, EventArgs.Empty);
        _knockback.GetKnockedBack(knockbackSource, knockbackMultiplier, _knockbackResist);
        DetectDeath();
    }
    
    public EnemySO GetEnemySO() => _enemySO;
    public void SetCanReceiveStagger(bool value) => CanReceiveStagger = value;
    public void SetCombat(bool value) => InCombat = value;

    public void ResetForTraining()
    {
        IsAlive = true;
        InCombat = false;
        LastAttacker = null;
        CurrentHealth = MaxHealth;

        if (_hitBox != null) _hitBox.enabled = true;
        if (_collisionBox != null) _collisionBox.enabled = true;

        InitializeStats();
        UpdateHealthBar();
        if (_healthBar != null)
            _healthBar.gameObject.SetActive(false);
    }

    private ICharacterEntity ResolveAttacker(Vector3 knockbackSource)
    {
        if (PartyManager.Instance == null)
            return null;

        ICharacterEntity best = null;
        float bestSq = 4f;

        foreach (ICharacterEntity member in PartyManager.Instance.Members)
        {
            if (member == null || !member.IsAlive) continue;

            float sq = ((Vector2)member.Transform.position - (Vector2)knockbackSource).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = member;
            }
        }

        return best;
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
        CurrentHealth = _enemySO.EnemyMaxHealth;

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
        _healthBar.value = CurrentHealth;
        _healthBar.gameObject.SetActive(false);
    }

    private void UpdateHealthBar()
    {
        if (_healthBar == null) return;
        _healthBar.gameObject.SetActive(true);
        _healthBar.value = CurrentHealth;
    }

    private void DetectDeath()
    {
        if (CurrentHealth <= 0f) Death();
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
