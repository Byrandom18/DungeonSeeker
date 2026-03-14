using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    public float DamageMulti = 1f;
    public float Cooldown = 0.5f;
    public bool RotationEnabled = true;

    public abstract void Attack();
    //public virtual void StartAttack() { }
    //public virtual void EndAttack() { }
}
