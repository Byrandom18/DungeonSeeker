using System;
using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private EnemySO _enemySO;
    private float _maxHealth;
    private float _currentHealth;

    public event EventHandler OnTakeHit;
    public event EventHandler OnDeath;

    private void Awake()
    {
        
    }

    private void Start()
    {
        if (_enemySO == null)
            Debug.LogError($"EnemySO is missing on {gameObject.name}");
        _maxHealth = _enemySO.EnemyHealth;
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
