using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class Sword : WeaponBase
{
    public event EventHandler OnSwordSwing;
    private PolygonCollider2D _attackBox;

    private TrailRenderer _trail;
    //public float Cooldown = 0.5f;
    //public bool RotationEnabled = false;

    protected override void Awake()
    {
        base.Awake();
        _attackBox = GetComponentInChildren<PolygonCollider2D>();
        _trail = GetComponentInChildren<TrailRenderer>();
    }

    private void Start()
    {
        if (_attackBox == null)
            Debug.LogError($"[Sword] Attack collider missing on {gameObject.name}");
    }

    

    public override void ApplyWeaponSO(WeaponSO data)
    {
        base.ApplyWeaponSO(data);
        if (_attackBox != null)
        {
            float scaleFactor = data.MeleeRange; // * sizeMod
            _attackBox.transform.localScale = Vector3.one * scaleFactor;
            if (_trail != null) _trail.widthMultiplier = scaleFactor;
        }
            
    }

    public override void Attack()
    {
        OnSwordSwing?.Invoke(this, EventArgs.Empty);
    }

    public void StartAttack() => _attackBox.enabled = true;
    public void EndAttack() => _attackBox.enabled = false;


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out EnemyDamage enemy))
        {
            float damage = DamageMulti * GetOwnerStat(StatType.AttackFlat);
            float critRate = GetOwnerStat(StatType.CritRate);
            float critDamage = GetOwnerStat(StatType.CritDamage);
            float knockbackMulti = WeaponData != null ? WeaponData.KnockbackMultiplier : 1f;
            enemy.TakeDamage(damage, critRate, critDamage, transform.position, knockbackMulti, true);
        }

        if (collision.TryGetComponent(out DestructibleEnvironment env))
            env.TakeDamage();
    }

    

    private float GetOwnerStat(StatType type)
    {
        if (Owner != null)
            return Owner.StatSystem.GetFinalValue(type);

        return 1f;
    }


}
