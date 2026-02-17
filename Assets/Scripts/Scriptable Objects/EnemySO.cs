using UnityEngine;

[CreateAssetMenu(fileName = "EnemySO", menuName = "Scriptable Objects/EnemySO")]
public class EnemySO : ScriptableObject
{
    public string EnemyName;
    public float EnemyHealth;
    public float EnemyAttackAmount;
}
