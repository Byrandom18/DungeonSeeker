using System.Collections.Generic;

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
        Active.Clear();
    }
}
