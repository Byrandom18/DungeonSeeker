using UnityEngine;

public struct ThreatSnapshot
{
    public bool HasIncomingThreat;
    public float IncomingThreatUrgency;
    public Vector2 ThreatDirection;
    public float NearestThreatDistance;
    public float NearestThreatTimeToImpact;

    public bool NearestEnemyAttacking;
    public float NearestEnemyBearing;

    public int AttackingEnemyCount;
    public int AttackingSelfCount;
    public bool AnyEnemyAttackingSelf;
    public float AttackingSelfUrgency;
}
