using UnityEngine;

public struct AbilityContext
{
    public Vector3 AimPosition;    // cursor position
    public ICharacterEntity Owner;
    public WeaponBase ActiveWeapon;
}

public abstract class AbilityBase
{
    public AbilitySO Data { get; private set; }
    public float Cooldown { get; private set; } 
    public bool IsReady => Cooldown <= 0f;

    protected ICharacterEntity Owner;

    public void Initialize(AbilitySO data, ICharacterEntity owner)
    {
        Data = data;
        Owner = owner;
    }

    public void Tick(float deltaTime)
    {
        if (Cooldown > 0f) Cooldown -= deltaTime;
    }

    
    public bool TryActivate(AbilityContext context)
    {
        if (!IsReady) return false;
        if (!CanActivate(context)) return false;
        if (!Owner.ConsumeMana(Data.ManaCost)) return false;
        Activate(context);
        Cooldown = Data.Cooldown;
        return true;
    }

    protected virtual bool CanActivate(AbilityContext context) => true;
    protected abstract void Activate(AbilityContext context);
}
