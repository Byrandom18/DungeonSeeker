using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class Sword : MonoBehaviour
{
    public event EventHandler OnSwordSwing;

    public float cooldown = 0.5f;
    public bool rotationEnabled = false; 

    public void Attack()
    {
        OnSwordSwing?.Invoke(this, EventArgs.Empty);
    }
}
