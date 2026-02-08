using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class Sword : MonoBehaviour
{
    [SerializeField] private float damageMulti;
    public event EventHandler OnSwordSwing;
    private PolygonCollider2D attackBox;
    public float cooldown = 0.5f;
    public bool rotationEnabled = false;

    private void Awake()
    {
        attackBox = GetComponentInChildren<PolygonCollider2D>();
    }

    private void Start()
    {
        CheckAllComponents();
    }


    public void Attack()
    {
        OnSwordSwing?.Invoke(this, EventArgs.Empty);
    }

    public void StartAttack()
    {
        attackBox.enabled = true;
    }

    public void EndAttack()
    {
        attackBox.enabled = false;
    }


    private void CheckAllComponents()
    {
        CheckComponent(attackBox, "Attack Collider");
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
            enemyDamage.TakeDamage(damage);
        }
    }

    private float CalculateDamage()
    {

        return damageMulti / 100 * PlayerStats.Instance.currentAtk;
    }

    
}
