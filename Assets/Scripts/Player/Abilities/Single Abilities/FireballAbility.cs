using UnityEngine;

public class FireballAbility : AbilityBase
{
    protected override bool CanActivate(AbilityContext ctx)
    {
        if (!Data.RequiresWeapon) return true;
        return ctx.ActiveWeapon?.WeaponData?.WeaponType == Data.RequiredWeapon;
    }

    protected override void Activate(AbilityContext ctx)
    {
        if (Data.EffectPrefab == null) return;

        Vector3 dir = (ctx.AimPosition - ctx.Owner.Transform.position).normalized;
        float atk = ctx.Owner.StatSystem.GetFinalValue(StatType.AttackFlat);

        GameObject go = Object.Instantiate(
            Data.EffectPrefab,
            ctx.Owner.Transform.position,
            Quaternion.AngleAxis(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, Vector3.forward)
        );

        if (go.TryGetComponent(out Projectile p))
        {
            p.Damage = atk * Data.DamageMultiplier;
            p.Speed = Data.ProjectileSpeed;
            p.Lifetime = Data.ProjectileLifetime;
            p.EnemyLaunch = false;
            p.KnockbackMultiplier = Data.KnockbackMultiplier;
            p.SetDirection(dir, ctx.Owner.Transform.position);
        }
    }
}
