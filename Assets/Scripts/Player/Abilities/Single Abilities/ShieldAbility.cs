using UnityEngine;

public class ShieldAbility : AbilityBase
{
    protected override bool CanActivate(AbilityContext ctx)
    {
        if (!Data.RequiresWeapon) return true;
        return ctx.ActiveWeapon?.WeaponData?.WeaponType == Data.RequiredWeapon;
    }

    protected override void Activate(AbilityContext ctx)
    {
        ICharacterEntity target = ResolveShieldTarget(ctx, Data);
        if (target == null) return;

        float amount = Data.ShieldAmount > 0f
            ? Data.ShieldAmount
            : Data.DamageMultiplier * ctx.Owner.StatSystem.GetFinalValue(StatType.DefenceFlat);

        float duration = Data.ShieldDuration > 0f ? Data.ShieldDuration : 4f;
        target.ApplyShield(amount, duration);
        AbilityTargetHelper.SpawnEffectAt(Data, target.Transform.position, target.Transform);
    }

    private static ICharacterEntity ResolveShieldTarget(AbilityContext ctx, AbilitySO data)
    {
        if (data.TargetType == AbilityTargetType.Self)
            return ctx.Owner;

        if (data.TargetType == AbilityTargetType.Ally)
        {
            return AbilityTargetHelper.FindAllyAtPosition(
                ctx.AimPosition,
                data.MaxRange > 0f ? data.MaxRange : 6f,
                ctx.Owner);
        }

        return ctx.Owner;
    }
}
