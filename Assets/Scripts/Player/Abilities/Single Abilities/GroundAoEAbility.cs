using UnityEngine;

public class GroundAoEAbility : AbilityBase
{
    protected override bool CanActivate(AbilityContext ctx)
    {
        if (!Data.RequiresWeapon) return true;
        return ctx.ActiveWeapon?.WeaponData?.WeaponType == Data.RequiredWeapon;
    }

    protected override void Activate(AbilityContext ctx)
    {
        if (Data.EffectPrefab == null) return;

        float spellDamageMod = 1f + ctx.Owner.StatSystem.GetFinalValue(StatType.SpellDamageMod) / 100f;
        float damagePerTick = ctx.Owner.StatSystem.GetFinalValue(StatType.AttackFlat)
            * spellDamageMod
            * Data.DamageMultiplier;

        float critRate = ctx.Owner.StatSystem.GetFinalValue(StatType.CritRate);
        float critDamage = ctx.Owner.StatSystem.GetFinalValue(StatType.CritDamage);
        float size = 1f + ctx.Owner.StatSystem.GetFinalValue(StatType.SizeMod) / 100f;

        Vector3 spawnPos = AbilityTargetHelper.ToGameplayPlane(ctx.AimPosition, ctx.Owner.Transform);
        GameObject go = Object.Instantiate(Data.EffectPrefab, spawnPos, Quaternion.identity);

        if (go.TryGetComponent(out PeriodicDamageZone zone))
        {
            zone.Configure(
                damagePerTick,
                Data.ZoneTickInterval > 0f ? Data.ZoneTickInterval : 0.5f,
                Data.ZoneDuration > 0f ? Data.ZoneDuration : 2f,
                Data.BaseScale * size,
                critRate,
                critDamage,
                enemyLaunch: false,
                staggerApply: Data.StaggerApply,
                knockbackMultiplier: Data.KnockbackMultiplier,
                damageSource: ctx.Owner.Transform.position);
            return;
        }

        if (go.TryGetComponent(out AreaAttack area))
        {
            area.Configure(
                damagePerTick,
                Data.BaseScale,
                Data.AreaOfEffect * size,
                enemyLaunch: false,
                staggerApply: Data.StaggerApply,
                knockbackMultiplier: Data.KnockbackMultiplier,
                damageSource: ctx.Owner.Transform.position);
        }
    }
}
