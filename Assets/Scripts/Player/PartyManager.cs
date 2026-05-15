using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks party members for enemy targeting and ally follow behaviour.
/// Characters register via <see cref="PlayerStats"/>; <see cref="Start"/> also rescans the scene.
/// </summary>
public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance { get; private set; }

    private readonly List<ICharacterEntity> _members = new List<ICharacterEntity>();
    private Transform _leaderTransform;
    private PlayerMovement _leaderMovement;

    public IReadOnlyList<ICharacterEntity> Members => _members;
    public Transform LeaderTransform => _leaderTransform;
    public PlayerMovement LeaderMovement => _leaderMovement;

    public event Action OnPartyChanged;

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

    private void Start()
    {
        RegisterAllCharactersInScene();
    }

    public void Register(ICharacterEntity entity)
    {
        if (entity == null) return;

        if (!_members.Contains(entity))
        {
            _members.Add(entity);
            OnPartyChanged?.Invoke();
        }

        if (entity is PlayerStats stats && stats.IsPrimaryPlayer)
            RegisterLeaderFrom(stats.Transform);
    }

    public void Unregister(ICharacterEntity entity)
    {
        if (entity == null) return;

        if (_members.Remove(entity))
            OnPartyChanged?.Invoke();

        if (entity.Transform == _leaderTransform)
        {
            _leaderTransform = null;
            _leaderMovement = null;
            TryAssignLeaderFromMembers();
        }
    }

    public void RegisterAllCharactersInScene()
    {
        var allStats = FindObjectsByType<PlayerStats>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (var stats in allStats)
            Register(stats);
    }

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

    private void RegisterLeaderFrom(Transform characterTransform)
    {
        if (characterTransform == null) return;

        _leaderTransform = characterTransform;
        _leaderMovement = characterTransform.GetComponent<PlayerMovement>();
    }

    private void TryAssignLeaderFromMembers()
    {
        foreach (var member in _members)
        {
            if (member is not PlayerStats stats || !stats.IsPrimaryPlayer) continue;
            RegisterLeaderFrom(member.Transform);
            return;
        }
    }
}
