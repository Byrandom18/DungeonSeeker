using UnityEngine;

public static class AbilityFactory
{
    public static AbilityBase Create(AbilitySO data, ICharacterEntity owner)
    {
        AbilityBase ability = data.AbilityType switch
        {
            AbilityType.Fireball => new FireballAbility(),
            AbilityType.Heal => new HealAbility(),
            AbilityType.Shield => new ShieldAbility(),
            AbilityType.GroundAoE => new GroundAoEAbility(),
            _ => null
        };

        ability?.Initialize(data, owner);
        return ability;
    }
}

public enum AbilityType
{
    Fireball,
    Heal,
    Shield,
    GroundAoE
}
