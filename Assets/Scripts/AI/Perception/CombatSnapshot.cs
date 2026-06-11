using UnityEngine;

public struct EnemySnapshot
{
    public Transform Transform;
    public EnemyDamage Damage;
    public float Distance;
    public float HealthPercent;
    public bool InCombat;
    public int FocusFireCount;
    public bool IsAttacking;
    public bool IsAttackingSelf;
}

public struct AllySnapshot
{
    public Transform Transform;
    public ICharacterEntity Entity;
    public float Distance;
    public float HealthPercent;
    public bool IsSelf;
}

public struct CombatSnapshot
{
    public Vector3 Position;
    public float HealthPercent;
    public float ManaPercent;
    public WeaponType EquippedWeapon;
    public float WeaponRange;
    public float OptimalWeaponRange;

    public EnemySnapshot[] Enemies;
    public AllySnapshot[] Allies;

    public int EnemyCount;
    public int ClusteredEnemyCount;
    public bool AnyAllyLowHealth;
    public float NearestEnemyDistance;
    public bool AnyEnemyInCombat;
    public int AttackingEnemyCount;
    public int AttackingSelfCount;
}

public struct TargetSelectionResult
{
    public Transform Target;
    public float Score;
    public bool HasTarget => Target != null;
}

public struct AbilityDecision
{
    public int SlotIndex;
    public Vector3 AimPosition;
    public Transform Target;
    public float Score;

    public bool IsValid => SlotIndex >= 0 && Score > 0f;
}
