using UnityEngine;

public class HealAbility : AbilityBase
{
    protected override bool CanActivate(AbilityContext ctx)
    {
        if (!Data.RequiresWeapon) return true;
        return ctx.ActiveWeapon?.WeaponData?.WeaponType == Data.RequiredWeapon;
    }

    protected override void Activate(AbilityContext ctx)
    {
        ICharacterEntity target = AbilityTargetHelper.FindAllyAtPosition(
            ctx.AimPosition,
            Data.MaxRange > 0f ? Data.MaxRange : 8f,
            ctx.Owner);

        float healAmount = Data.HealAmount > 0f
            ? Data.HealAmount
            : Data.DamageMultiplier * ctx.Owner.StatSystem.GetFinalValue(StatType.AttackFlat);

        target.Heal(healAmount);
        AbilityTargetHelper.SpawnEffectAt(Data, target.Transform.position, target.Transform);
    }
}
