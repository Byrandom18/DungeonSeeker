using System;
using UnityEngine;

public class DestructibleEnvironment : MonoBehaviour
{
    public event EventHandler OnDestroyEnvironment;
    [SerializeField] private int _health = 1;
    public void TakeDamage()
    {
        _health -= 1;
        if (_health <= 0) Death();
    }

    private void Death()
    {
        OnDestroyEnvironment?.Invoke(this, EventArgs.Empty);
        Destroy(gameObject);
        NavMeshSurfaceManager.Instance.RebakeNavMeshSurface();
    }
}
