using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Area that damages on enter (first tick immediately) then again every _tickInterval seconds
/// while the target stays inside the trigger.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PeriodicDamageZone : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float _damagePerTick = 10f;
    [SerializeField] private float _knockbackMultiplier = 1;
    [SerializeField] private bool _staggerApply;
    [SerializeField] private bool _enemyLaunch;
    [SerializeField] private float _critRate;
    [SerializeField] private float _critDamage;

    [Header("Timing")]
    [SerializeField] private float _tickInterval = 0.5f;

    [Header("Lifetime")]
    [SerializeField] private float _duration = 1f;

    private Vector3 _damageSource;
    private readonly Dictionary<Collider2D, float> _nextTickTime = new Dictionary<Collider2D, float>();

    private Collider2D _zoneCollider;

    private void Awake()
    {
        _zoneCollider = GetComponent<Collider2D>();
        _zoneCollider.isTrigger = true;
        _damageSource = transform.position;
    }

    private void Update()
    {
        if (_nextTickTime.Count == 0) return;

        // Collect keys to avoid modifying the dictionary during iteration
        var colliders = new List<Collider2D>(_nextTickTime.Keys);
        foreach (var col in colliders)
        {
            if (col == null || !col.enabled)
            {
                _nextTickTime.Remove(col);
                continue;
            }

            if (Time.time < _nextTickTime[col]) continue;

            ApplyDamageToTarget(col);
            _nextTickTime[col] = Time.time + _tickInterval;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.enabled) return;

        ApplyDamageToTarget(collision);

        float next = Time.time + _tickInterval;
        _nextTickTime[collision] = next;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        _nextTickTime.Remove(collision);
    }

    /// <summary>
    /// Call after instantiate if the zone should use owner position for knockback direction.
    /// </summary>
    public void Configure( // TODO: make constructor
        float damagePerTick,
        float tickInterval,
        float duration,
        float size,
        float critRate,
        float critDamage,
        bool enemyLaunch,
        bool staggerApply,
        float knockbackMultiplier,
        Vector3 damageSource)
    {
        _damagePerTick = damagePerTick;
        _tickInterval = Mathf.Max(0.05f, tickInterval);
        _duration = duration;
        transform.localScale = Vector2.one * size;
        _critRate = critRate;
        _critDamage = critDamage;
        _enemyLaunch = enemyLaunch;
        _staggerApply = staggerApply;
        _knockbackMultiplier = knockbackMultiplier;
        _damageSource = damageSource;

        Destroy(gameObject, _duration);
    }

    private void ApplyDamageToTarget(Collider2D collision)
    {
        if (_enemyLaunch)
        {
            if (collision.TryGetComponent(out ICharacterEntity ch) && ch.IsAlive)
                ch.TakeDamage(_damagePerTick, _damageSource, _knockbackMultiplier);
        }
        else
        {
            if (collision.TryGetComponent(out EnemyDamage enemy) && enemy.IsAlive)
                enemy.TakeDamage(_damagePerTick, _critRate, _critDamage, _damageSource, _knockbackMultiplier, _staggerApply);
        }
        if (collision.TryGetComponent(out DestructibleEnvironment env))
            env.TakeDamage();
    }

    private void OnDisable()
    {
        _nextTickTime.Clear();
    }
}
