using UnityEngine;

public static class AbilityFactory
{
    public static AbilityBase Create(AbilitySO data, ICharacterEntity owner)
    {
        AbilityBase ability = data.AbilityType switch
        {
            //AbilityType.Dash => new DashAbility(dashForce: 15f),
            AbilityType.Fireball => new FireballAbility(),
            //AbilityType.Shield => new ShieldAbility(),
            //AbilityType.Heal => new HealAbility(),
            _ => null
        };

        ability?.Initialize(data, owner);
        return ability;
    }
}

public enum AbilityType
{
    Fireball
}