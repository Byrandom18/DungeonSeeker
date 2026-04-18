using UnityEngine;

/// <summary>
/// A common interface for all combat members of the group —
/// player and bot allies. Enemies only work with this
/// interface and don't know about specific classes.
/// </summary>
public interface ICharacterEntity
{
    StatSystem StatSystem { get; }
    bool IsAlive { get; }
    Transform Transform { get; }

    void TakeDamage(float damage, Vector3 knockbackSource, float knockbackMultiplier);
    void Heal(float amount);
}
