using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class Sword : WeaponBase
{
    public event EventHandler OnSwordSwing;
    private PolygonCollider2D _attackBox;
    //public float Cooldown = 0.5f;
    //public bool RotationEnabled = false;

    private void Awake()
    {
        _attackBox = GetComponentInChildren<PolygonCollider2D>();
    }

    private void Start()
    {
        CheckAllComponents();
    }


    public override void Attack()
    {
        OnSwordSwing?.Invoke(this, EventArgs.Empty);
    }

    public void StartAttack()
    {
        _attackBox.enabled = true;
    }

    public void EndAttack()
    {
        _attackBox.enabled = false;
    }


    private void CheckAllComponents()
    {
        CheckComponent(_attackBox, "Attack Collider");
    }

    private void CheckComponent<T>(T component, string componentName) where T : Component
    {
        if (component == null)
            Debug.LogError($"{componentName} is missing on {gameObject.name}");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.TryGetComponent(out EnemyDamage enemyDamage))
        {
            float damage = CalculateDamage();
            enemyDamage.TakeDamage(damage, transform.position, PlayerStats.Instance.KnockbackMultiplier, true);
        }
        if (collision.TryGetComponent(out DestructibleEnvironment distructibleEnvironment))
        {
            distructibleEnvironment.TakeDamage();
        }
    }

    private float CalculateDamage() 
    {

        return DamageMulti * PlayerStats.Instance.CurrentAtk;
    }

    
}
