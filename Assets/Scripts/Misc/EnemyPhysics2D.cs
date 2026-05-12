using UnityEngine;

/// <summary>
/// Shared 2D overlap queries for colliders on the "Enemy" layer (Unity 6+ API).
/// </summary>
public static class EnemyPhysics2D
{
    private const string EnemyLayerName = "Enemy";

    private static readonly ContactFilter2D EnemyFilter = BuildEnemyFilter();

    private static ContactFilter2D BuildEnemyFilter()
    {
        var filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask(EnemyLayerName));
        filter.useTriggers = true;
        return filter;
    }

    public static int OverlapCircle(Vector2 center, float radius, Collider2D[] results) =>
        Physics2D.OverlapCircle(center, radius, EnemyFilter, results);
}
