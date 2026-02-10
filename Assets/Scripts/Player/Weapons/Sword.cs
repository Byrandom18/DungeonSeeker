using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class Sword : MonoBehaviour
{
    [SerializeField] private float _damageMulti;
    public event EventHandler OnSwordSwing;
    private PolygonCollider2D _attackBox;
    public float Cooldown = 0.5f;
    public bool RotationEnabled = false;

    private void Awake()
    {
        _attackBox = GetComponentInChildren<PolygonCollider2D>();
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
            enemyDamage.TakeDamage(damage);
        }
    }

    private float CalculateDamage()
    {

        return _damageMulti / 100 * PlayerStats.Instance.CurrentAtk;
    }

    
}
