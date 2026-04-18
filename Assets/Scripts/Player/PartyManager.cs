using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A singleton that keeps all the members of the group alive.
///
/// Enemies use GetNearestTarget(from) instead of a direct link
/// to PlayerMovement.Instance — this allows them to attack any
/// member of the group: a player or a bot.
///
/// Characters register themselves via Register/Unregister
/// in their onenables/ondisables.
/// </summary>
public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance { get; private set; }

    private readonly List<ICharacterEntity> _members = new List<ICharacterEntity>();

    /// <summary>Only alive chars</summary>
    public IReadOnlyList<ICharacterEntity> Members => _members;

    public event Action OnPartyChanged;

    // == Lifecycle ==============================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // == Registration ============================================================

    public void Register(ICharacterEntity entity)
    {
        if (!_members.Contains(entity))
        {
            _members.Add(entity);
            OnPartyChanged?.Invoke();
        }
    }

    public void Unregister(ICharacterEntity entity)
    {
        if (_members.Remove(entity))
            OnPartyChanged?.Invoke();
    }

    // == Запросы для врагов ====================================================

    /// <summary>
    /// Returns the closest living member of the group to the point from.
    /// If everyone is dead, it returns null.
    /// </summary>
    public ICharacterEntity GetNearestTarget(Vector3 from)
    {
        ICharacterEntity nearest = null;
        float minDist = float.MaxValue;

        foreach (var member in _members)
        {
            if (!member.IsAlive) continue;

            float dist = Vector3.Distance(from, member.Transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = member;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Returns the position of the nearest living member of the group.
    /// If the group is empty, it returns Vector3.zero.
    /// </summary>
    public Vector3 GetNearestTargetPosition(Vector3 from)
    {
        var target = GetNearestTarget(from);
        return target != null ? target.Transform.position : Vector3.zero;
    }

    public bool HasAnyAlive()
    {
        foreach (var m in _members)
            if (m.IsAlive) return true;
        return false;
    }
}
