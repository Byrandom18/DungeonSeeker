using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float currentHealth;
    
    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        DetectDeath();
    }

    private void DetectDeath()
    {
        if (currentHealth <= 0)
        {
            Death();
        }
    }

    private void Death()
    {
        Debug.Log("Enemy dead");
    }
}
