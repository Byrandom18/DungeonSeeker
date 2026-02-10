using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float _maxHealth;
    [SerializeField] private float _currentHealth;
    
    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
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
        Debug.Log("Enemy dead");
    }
}
