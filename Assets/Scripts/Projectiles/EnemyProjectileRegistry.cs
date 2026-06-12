using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks active enemy-launched projectiles for ally threat perception.
/// </summary>
public static class EnemyProjectileRegistry
{
    private static readonly List<Projectile> Active = new List<Projectile>(32);

    public static IReadOnlyList<Projectile> ActiveProjectiles => Active;

    public static void Register(Projectile projectile)
    {
        if (projectile == null || !projectile.EnemyLaunch)
            return;

        if (!Active.Contains(projectile))
            Active.Add(projectile);
    }

    public static void Unregister(Projectile projectile)
    {
        if (projectile == null)
            return;

        Active.Remove(projectile);
    }

    public static void Clear()
    {
        if (Active.Count == 0)
            return;

        var snapshot = new Projectile[Active.Count];
        Active.CopyTo(snapshot);
        Active.Clear();

        for (int i = 0; i < snapshot.Length; i++)
        {
            if (snapshot[i] != null)
                Object.Destroy(snapshot[i].gameObject);
        }
    }
}
