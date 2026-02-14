using System;
using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float _maxHealth;
    [SerializeField] private float _currentHealth;

    public event EventHandler OnTakeHit;
    public event EventHandler OnDeath;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        OnTakeHit?.Invoke(this, EventArgs.Empty);
        DetectDeath();
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
