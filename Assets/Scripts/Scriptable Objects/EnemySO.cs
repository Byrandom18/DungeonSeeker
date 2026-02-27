using UnityEngine;

[CreateAssetMenu(fileName = "EnemySO", menuName = "Scriptable Objects/EnemySO")]
public class EnemySO : ScriptableObject
{
    [Header("Name")]
    public string EnemyName;

    [Header("Stats settings")]
    public float EnemyMaxHealth;
    public float EnemyBaseAttack;

    [Header("Roaming settings")]
    public bool EnableRoam = true;
    public bool EnableChangeStartPos = true;
    public float RoamSpeed = 1.5f;
    public float RoamingDistanceMax = 7f;
    public float RoamingDistanceMin = 1f;
    public float RoamingTimerMax = 3f;
    public float IdleDuration = 2f;

    [Header("Chase settings")]
    public bool EnableChase = true;
    public float ChasingSpeed = 3f;
    public float ChasingDistance = 5f;

    [Header("Attack settings")]
    public bool EnableAttack = true;
    public float AttackDistance = 2f;
    public float AttackRate = 3f;

    public bool EnableTouchDamage = false;

    [Header("Stagger settings")]
    public bool StaggerImmune = false;
    public float StaggerDuration = 1f;

    [Header("Knockback settings")]
    public bool EnableKnockback = true;
    public float KnockbackMultiplier = 1;
    public float KnockbackResist = 0;
}
