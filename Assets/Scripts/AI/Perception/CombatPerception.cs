using UnityEngine;

[DisallowMultipleComponent]
public class CombatPerception : MonoBehaviour
{
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private ActiveWeapon _activeWeapon;
    [SerializeField] private float _scanRadius = 12f;
    [SerializeField] private float _clusterRadius = 3f;
    [SerializeField] private float _allyLowHealthThreshold = 0.4f;

    private readonly Collider2D[] _enemyBuffer = new Collider2D[32];
    private readonly EnemySnapshot[] _enemySnapshots = new EnemySnapshot[32];
    private readonly AllySnapshot[] _allySnapshots = new AllySnapshot[16];

    public float ScanRadius => _scanRadius;
    public CombatSnapshot LastSnapshot { get; private set; }

    private void Awake()
    {
        if (_stats == null)
            _stats = GetComponent<PlayerStats>();
        if (_activeWeapon == null)
            _activeWeapon = GetComponentInChildren<ActiveWeapon>(true);
    }

    public CombatSnapshot BuildSnapshot()
    {
        Vector3 position = transform.position;
        float healthPercent = 1f;
        float manaPercent = 1f;

        if (_stats != null)
        {
            healthPercent = _stats.MaxHealth > 0f ? _stats.Health / _stats.MaxHealth : 0f;
            manaPercent = _stats.MaxMana > 0f ? _stats.Mana / _stats.MaxMana : 0f;
        }

        WeaponType weaponType = WeaponType.Sword;
        float weaponRange = 1.5f;
        float optimalRange = 1f;

        WeaponBase weapon = _activeWeapon != null ? _activeWeapon.GetActiveWeapon() : null;
        if (weapon != null && weapon.WeaponData != null)
        {
            weaponType = weapon.WeaponData.WeaponType;
            weaponRange = GetWeaponRange(weapon.WeaponData);
            optimalRange = GetOptimalRange(weapon.WeaponData);
        }

        int enemyCount = GatherEnemies(position);
        int allyCount = GatherAllies(position);

        int clustered = CountClusteredEnemies(enemyCount);
        bool anyAllyLow = AnyAllyBelowThreshold(allyCount, _allyLowHealthThreshold);
        bool anyInCombat = AnyEnemyInCombat(enemyCount);
        float nearestDist = GetNearestEnemyDistance(enemyCount);

        LastSnapshot = new CombatSnapshot
        {
            Position = position,
            HealthPercent = healthPercent,
            ManaPercent = manaPercent,
            EquippedWeapon = weaponType,
            WeaponRange = weaponRange,
            OptimalWeaponRange = optimalRange,
            Enemies = CopyEnemySlice(enemyCount),
            Allies = CopyAllySlice(allyCount),
            EnemyCount = enemyCount,
            ClusteredEnemyCount = clustered,
            AnyAllyLowHealth = anyAllyLow,
            NearestEnemyDistance = nearestDist,
            AnyEnemyInCombat = anyInCombat
        };

        return LastSnapshot;
    }

    private int GatherEnemies(Vector3 origin)
    {
        int count = EnemyPhysics2D.OverlapCircle(origin, _scanRadius, _enemyBuffer);
        int written = 0;

        for (int i = 0; i < count && written < _enemySnapshots.Length; i++)
        {
            Collider2D c = _enemyBuffer[i];
            if (c == null) continue;
            if (!c.TryGetComponent(out EnemyDamage ed) || !ed.IsAlive) continue;

            float dist = Vector2.Distance(origin, c.transform.position);
            _enemySnapshots[written++] = new EnemySnapshot
            {
                Transform = c.transform,
                Damage = ed,
                Distance = dist,
                HealthPercent = ed.HealthPercent,
                InCombat = ed.InCombat,
                FocusFireCount = PartyCombatCoordinator.GetFocusCount(c.transform)
            };
        }

        return written;
    }

    private int GatherAllies(Vector3 origin)
    {
        int written = 0;

        if (PartyManager.Instance == null)
            return 0;

        foreach (ICharacterEntity member in PartyManager.Instance.Members)
        {
            if (written >= _allySnapshots.Length) break;
            if (member == null || !member.IsAlive) continue;

            float maxHealth = member.StatSystem.GetFinalValue(StatType.HealthFlat);
            float health = maxHealth;
            if (member is PlayerStats ps)
                health = ps.Health;

            float healthPercent = maxHealth > 0f ? health / maxHealth : 1f;
            float dist = Vector2.Distance(origin, member.Transform.position);

            _allySnapshots[written++] = new AllySnapshot
            {
                Transform = member.Transform,
                Entity = member,
                Distance = dist,
                HealthPercent = healthPercent,
                IsSelf = member.Transform == transform
            };
        }

        return written;
    }

    private int CountClusteredEnemies(int enemyCount)
    {
        if (enemyCount <= 1)
            return enemyCount;

        int bestCluster = 0;

        for (int i = 0; i < enemyCount; i++)
        {
            Vector2 center = _enemySnapshots[i].Transform.position;
            int cluster = 0;

            for (int j = 0; j < enemyCount; j++)
            {
                if (Vector2.Distance(center, _enemySnapshots[j].Transform.position) <= _clusterRadius)
                    cluster++;
            }

            if (cluster > bestCluster)
                bestCluster = cluster;
        }

        return bestCluster;
    }

    private bool AnyAllyBelowThreshold(int allyCount, float threshold)
    {
        for (int i = 0; i < allyCount; i++)
        {
            if (_allySnapshots[i].IsSelf) continue;
            if (_allySnapshots[i].HealthPercent < threshold)
                return true;
        }

        return false;
    }

    private bool AnyEnemyInCombat(int enemyCount)
    {
        for (int i = 0; i < enemyCount; i++)
        {
            if (_enemySnapshots[i].InCombat)
                return true;
        }

        return false;
    }

    private float GetNearestEnemyDistance(int enemyCount)
    {
        float nearest = float.MaxValue;

        for (int i = 0; i < enemyCount; i++)
            nearest = Mathf.Min(nearest, _enemySnapshots[i].Distance);

        return nearest == float.MaxValue ? -1f : nearest;
    }

    private EnemySnapshot[] CopyEnemySlice(int count)
    {
        var slice = new EnemySnapshot[count];
        for (int i = 0; i < count; i++)
            slice[i] = _enemySnapshots[i];
        return slice;
    }

    private AllySnapshot[] CopyAllySlice(int count)
    {
        var slice = new AllySnapshot[count];
        for (int i = 0; i < count; i++)
            slice[i] = _allySnapshots[i];
        return slice;
    }

    public static float GetWeaponRange(WeaponSO data)
    {
        return data.WeaponType switch
        {
            WeaponType.Sword => data.MeleeRange > 0f ? data.MeleeRange : 1.5f,
            WeaponType.Bow => 10f,
            WeaponType.Staff => 8f,
            WeaponType.Talisman => 6f,
            _ => 2f
        };
    }

    public static float GetOptimalRange(WeaponSO data)
    {
        return data.WeaponType switch
        {
            WeaponType.Sword => data.MeleeRange > 0f ? data.MeleeRange * 0.8f : 1.2f,
            WeaponType.Bow => 6f,
            WeaponType.Staff => 5f,
            WeaponType.Talisman => 4f,
            _ => 2f
        };
    }
}
