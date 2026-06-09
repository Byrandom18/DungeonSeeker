using System;
using UnityEngine;

/// <summary>
/// Forwards combat events from <see cref="PlayerStats"/> and spawned enemies to <see cref="AllyPositionAgent"/>.
/// Add to the ally prefab used in training and in-game when ML rewards are needed.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AllyPositionAgent))]
public class AllyMLRewardForwarder : MonoBehaviour
{
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private AllyPositionAgent _agent;

    private void Awake()
    {
        if (_stats == null)
            _stats = GetComponent<PlayerStats>();
        if (_agent == null)
            _agent = GetComponent<AllyPositionAgent>();
    }

    private void OnEnable()
    {
        if (_stats != null)
        {
            _stats.OnCombatDamage += HandleCombatDamage;
            _stats.OnPlayerDeath += HandleSelfDeath;
        }

        AllyTrainingEnvironment.OnEnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        if (_stats != null)
        {
            _stats.OnCombatDamage -= HandleCombatDamage;
            _stats.OnPlayerDeath -= HandleSelfDeath;
        }

        AllyTrainingEnvironment.OnEnemyKilled -= HandleEnemyKilled;
    }

    private void HandleCombatDamage(float healthDamage, float shieldAbsorbed)
    {
        if (_agent == null) return;

        if (healthDamage > 0f)
            _agent.ReportDamageTaken(healthDamage);

        if (shieldAbsorbed > 0f)
            _agent.ReportShieldAbsorbed(shieldAbsorbed);
    }

    private void HandleSelfDeath(object sender, EventArgs e)
    {
        _agent?.ReportSelfDeath();
    }

    private void HandleEnemyKilled(EnemyDamage enemy, ICharacterEntity killer)
    {
        if (_agent == null || _stats == null || enemy == null) return;
        if (!ReferenceEquals(killer, _stats)) return;

        _agent.ReportEnemyKill(enemy.MaxHealth);
    }
}
