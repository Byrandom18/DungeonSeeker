using UnityEngine;

/// <summary>
/// Rough ally skill usage: when an enemy is in range, periodically tries each slot.
/// Requires <see cref="AbilitySystem"/> on the same GameObject as this component.
/// Assign the ally's own <see cref="ActiveWeapon"/> on <see cref="AbilitySystem"/>.
/// </summary>
[DisallowMultipleComponent]
public class AllyAbilityBrain : MonoBehaviour
{
    [SerializeField] private AbilitySystem _abilitySystem;
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private float _scanRadius = 10f;
    [SerializeField] private float _tryInterval = 0.6f;

    private float _cooldownTick;
    private readonly Collider2D[] _buffer = new Collider2D[32];

    private void Awake()
    {
        if (_abilitySystem == null)
            _abilitySystem = GetComponent<AbilitySystem>();
        if (_stats == null)
            _stats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (_abilitySystem == null || _stats == null || !_stats.IsAlive)
            return;

        _cooldownTick -= Time.deltaTime;
        if (_cooldownTick > 0f) return;
        _cooldownTick = _tryInterval;

        if (!TryGetNearestEnemy(out Vector3 aimWorld))
            return;

        int n = _abilitySystem.AbilitySlotCount;
        for (int i = 0; i < n; i++)
            _abilitySystem.UseAbility(i, aimWorld);
    }

    private bool TryGetNearestEnemy(out Vector3 position)
    {
        position = default;
        int n = EnemyPhysics2D.OverlapCircle(transform.position, _scanRadius, _buffer);

        Transform best = null;
        float bestSq = float.MaxValue;

        for (int i = 0; i < n; i++)
        {
            Collider2D c = _buffer[i];
            if (c == null) continue;
            if (!c.TryGetComponent(out EnemyDamage ed) || !ed.IsAlive) continue;

            float sq = (c.transform.position - transform.position).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = c.transform;
            }
        }

        if (best == null)
            return false;

        position = best.position;
        return true;
    }
}
