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
        float spellDamageMod = 1 + ctx.Owner.StatSystem.GetFinalValue(StatType.SpellDamageMod) / 100;
        float damage = ctx.Owner.StatSystem.GetFinalValue(StatType.AttackFlat) * spellDamageMod * Data.DamageMultiplier;
        float critRate = ctx.Owner.StatSystem.GetFinalValue(StatType.CritRate);
        float critDamage = ctx.Owner.StatSystem.GetFinalValue(StatType.CritDamage);
        float size = 1 + ctx.Owner.StatSystem.GetFinalValue(StatType.SizeMod) / 100;
        Vector3 spawnPosition = ctx.Owner.Transform.position;
        spawnPosition.y += 0.5f;

        GameObject go = Object.Instantiate(
            Data.EffectPrefab,
            spawnPosition,
            Quaternion.AngleAxis(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, Vector3.forward)
        );

        
        if (go.TryGetComponent(out Projectile p))
        {
            var config = new ProjectileConfig
            {
                Speed =                  Data.ProjectileSpeed,
                Lifetime =               Data.ProjectileLifetime,
                Damage =                 damage,
                CritRate =               critRate,
                CritDamage =             critDamage,
                Penetrate =              Data.Penetrate,
                KnockbackMultiplier =    Data.KnockbackMultiplier,
                Size =                   Data.BaseScale * size,
                AreaModifier =           Data.AreaOfEffect * size,
                StaggerApply =           Data.StaggerApply,
                EnemyLaunch =            false,
                Direction =              dir,
                StartPosition =          ctx.Owner.Transform.position
            };

            p.Configure(config);
        }
    }
}
