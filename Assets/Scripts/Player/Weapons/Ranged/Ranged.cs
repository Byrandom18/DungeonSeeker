using UnityEngine;
using System;

public class Ranged : WeaponBase
{
    // from WeaponSO via ApplyWeaponSO
    private GameObject _projectilePrefab;
    private float      _projectileSpeed;
    private float      _projectileLifetime;
    private float      _sizeMultiplier;
    private int        _projectileCount;
    private float      _spreadAngle;
    private float      _penetrate;
    private bool       _applyStagger;
    private float      _knockbackMultiplier;

    public event EventHandler OnRangedAttack;

    public override void ApplyWeaponSO(WeaponSO data)
    {
        base.ApplyWeaponSO(data);

        _projectilePrefab    = data.ProjectilePrefab;
        _projectileSpeed     = data.ProjectileSpeed;
        _projectileLifetime  = data.ProjectileLifetime;
        _sizeMultiplier      = data.SizeMultiplier;
        _projectileCount     = Mathf.Max(1, data.ProjectileCount);
        _spreadAngle         = data.SpreadAngle;
        _penetrate           = data.Penetrate;
        _applyStagger        = data.ApplyStagger;
        _knockbackMultiplier = data.KnockbackMultiplier;
    }



    public override void Attack()
    {
        if (_projectilePrefab == null)
        {
            Debug.LogWarning("[Ranged] ProjectilePrefab not specified in WeaponSO");
            return;
        }

        OnRangedAttack?.Invoke(this, EventArgs.Empty);

        Vector3 mousePos = GameInput.Instance.GetMousePosition();
        Vector3 playerScreen = PlayerMovement.Instance.GetPlayerScreenPosition();
        Vector3 baseDirection = (mousePos - playerScreen).normalized;
        float damage = GetOwnerAttack() * DamageMulti;

        SpawnProjectiles(baseDirection, damage);
    }


    // =========== Projectiles =================================================================
    private void SpawnProjectiles(Vector3 baseDirection, float damage)
    {
        if (_projectileCount == 1)
        {
            SpawnSingle(baseDirection, damage);
            return;
        }

        // Multiple projectiles with a spread around the baseDirection
        float halfSpread = _spreadAngle * (_projectileCount - 1) / 2f;
        float baseAngleDeg = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;

        for (int i = 0; i < _projectileCount; i++)
        {
            float offsetDeg = -halfSpread + i * _spreadAngle;
            float finalDeg = baseAngleDeg + offsetDeg;
            Vector2 dir = new Vector2(
                Mathf.Cos(finalDeg * Mathf.Deg2Rad),
                Mathf.Sin(finalDeg * Mathf.Deg2Rad));
            SpawnSingle(dir, damage);
        }
    }


    private void SpawnSingle(Vector2 direction, float damage)
    {
        float   angle      = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector3 spawnPos   = transform.position;

        GameObject go = Instantiate(_projectilePrefab, spawnPos,
                                    Quaternion.AngleAxis(angle, Vector3.forward));
        go.transform.localScale = Vector3.one * _sizeMultiplier;

        if (go.TryGetComponent(out Projectile p))
        {
            p.Speed              = _projectileSpeed;
            p.Lifetime           = _projectileLifetime;
            p.Damage             = damage;
            p.Penetrate          = _penetrate;
            p.KnockbackMultiplier = _knockbackMultiplier;
            p.StaggerApply       = _applyStagger;
            p.EnemyLaunch        = false;
            p.SetDirection(direction, transform.position);
        }
    }

    private float GetOwnerAttack()
    {
        if (Owner != null)
            return Owner.StatSystem.GetFinalValue(StatType.AttackFlat);
        return PlayerStats.Instance != null ? PlayerStats.Instance.Attack : 1f;
    }
}
