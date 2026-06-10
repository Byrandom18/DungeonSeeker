using TMPro;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// ML-Agents policy for ally combat movement: distance, retreat, strafe, and dodge shaping.
/// Target/ability selection stays in utility AI + Unity Behavior.
///
/// Behavior Parameters (on same GameObject):
///   Behavior Name: AllyPosition
///   Vector Observation Space Size: 17
///   Continuous Actions: 4
///   Behavior Type: Default (training) / Heuristic Only (no model) / Inference Only (onnx)
/// Also add DecisionRequester (Decision Period = 5).
/// </summary>
[RequireComponent(typeof(AllyAIBrain))]
[RequireComponent(typeof(AllyMLBridge))]
[RequireComponent(typeof(AllyMLMovementModifier))]
public class AllyPositionAgent : Agent
{
    public const int ObservationSize = 17;
    public const int ContinuousActionSize = 4;
    public const string BehaviorName = "AllyPosition";

    [SerializeField] private AllyAIBrain _brain;
    [SerializeField] private AllyMLBridge _mlBridge;
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private ThreatPerception _threatPerception;

    [Header("Reward tuning")]
    [SerializeField] private float _stepPenalty = 0.001f;
    [SerializeField] private float _distanceRewardScale = 0.01f;
    [SerializeField] private float _followLeaderPenaltyScale = 0.005f;
    [SerializeField] private float _maxLeaderFollowDistance = 6f;
    [SerializeField] private float _evadeWhileAttackingReward = 0.003f;
    [SerializeField] private float _nearMissReward = 0.08f;
    [SerializeField] private float _nearMissDistance = 1.5f;

    [SerializeField] private TMP_Text _rewardText;

    private CombatPerception _perception;
    private bool _hadHighThreatLastStep;
    private bool _tookDamageThisStep;

    public override void Initialize()
    {
        if (_brain == null)
            _brain = GetComponent<AllyAIBrain>();
        if (_mlBridge == null)
            _mlBridge = GetComponent<AllyMLBridge>();
        if (_stats == null)
            _stats = GetComponent<PlayerStats>();
        if (_threatPerception == null)
            _threatPerception = GetComponent<ThreatPerception>();
        if (_threatPerception == null)
            _threatPerception = gameObject.AddComponent<ThreatPerception>();

        _perception = GetComponent<CombatPerception>();

        AllyTrainingEnvironment.Instance?.RegisterAgent(this);
    }

    public override void OnEpisodeBegin()
    {
        if (AllyTrainingEnvironment.Instance != null)
            AllyTrainingEnvironment.Instance.ResetEpisode(this);
        else
            _mlBridge?.SetMode(AllyMLMode.Heuristic);

        _hadHighThreatLastStep = false;
        _tookDamageThisStep = false;
        _brain?.RefreshSnapshot();
        _stats?.ResetVisualsForTraining();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        CombatSnapshot snapshot = _brain != null ? _brain.Snapshot : default;
        ThreatSnapshot threat = _threatPerception != null
            ? _threatPerception.BuildSnapshot(transform.position, snapshot)
            : default;

        sensor.AddObservation(_brain != null ? _brain.HealthPercent : 1f);
        sensor.AddObservation(_brain != null ? _brain.ManaPercent : 1f);
        sensor.AddObservation(GetShieldPercent());
        sensor.AddObservation(NormalizeCount(snapshot.EnemyCount, 8));
        sensor.AddObservation(NormalizeDistance(snapshot.NearestEnemyDistance, 12f));
        sensor.AddObservation(NormalizeCount(snapshot.ClusteredEnemyCount, 6));
        sensor.AddObservation(snapshot.AnyAllyLowHealth ? 1f : 0f);
        sensor.AddObservation(snapshot.AnyEnemyInCombat ? 1f : 0f);
        sensor.AddObservation(GetLeaderDistanceNormalized());
        sensor.AddObservation(_brain != null ? _brain.AttackDistance / 12f : 0.1f);
        sensor.AddObservation(GetProfileObservation());
        sensor.AddObservation(GetWeaponTypeObservation());

        sensor.AddObservation(threat.NearestEnemyAttacking ? 1f : 0f);
        sensor.AddObservation(threat.NearestEnemyBearing);
        sensor.AddObservation(threat.IncomingThreatUrgency);
        sensor.AddObservation(NormalizeSigned(threat.ThreatDirection.x));
        sensor.AddObservation(NormalizeSigned(threat.ThreatDirection.y));
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (_mlBridge == null)
            return;

        float preferredDistance = Mathf.Clamp01(actions.ContinuousActions[0]);
        float retreatUrgency = Mathf.Clamp01(actions.ContinuousActions[1]);
        float strafeDirection = Mathf.Clamp01(actions.ContinuousActions[2]);
        float strafeIntensity = Mathf.Clamp01(actions.ContinuousActions[3]);

        _mlBridge.SetMLActions(preferredDistance, retreatUrgency, strafeDirection, strafeIntensity);
        ApplyShapingRewards();
        _tookDamageThisStep = false;

        if (_brain != null && _brain.HealthPercent <= 0f)
            ReportSelfDeath();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuous = actionsOut.ContinuousActions;
        CombatSnapshot snapshot = _brain != null ? _brain.Snapshot : default;
        ThreatSnapshot threat = _threatPerception != null
            ? _threatPerception.LastSnapshot
            : default;

        float preferred = snapshot.EnemyCount > 0 ? 0.55f : 0.35f;
        float retreat = 0f;
        float strafeDir = 0.5f;
        float strafeIntensity = 0f;

        if (_brain != null)
        {
            float shieldPct = GetShieldPercent();
            if (_brain.HealthPercent < 0.25f && shieldPct < 0.1f)
                retreat = 0.9f;
            else if (_brain.HealthPercent < 0.45f && snapshot.EnemyCount >= 3)
                retreat = 0.6f;
        }

        if (threat.HasIncomingThreat)
        {
            strafeIntensity = Mathf.Clamp01(threat.IncomingThreatUrgency);
            Vector2 lateral = new Vector2(-threat.ThreatDirection.y, threat.ThreatDirection.x);
            strafeDir = lateral.x >= 0f ? 0.75f : 0.25f;
        }
        else if (snapshot.EnemyCount > 0)
        {
            strafeIntensity = 0.35f;
            strafeDir = Mathf.PingPong(Time.time * 0.5f, 1f);
        }

        continuous[0] = preferred;
        continuous[1] = retreat;
        continuous[2] = strafeDir;
        continuous[3] = strafeIntensity;
    }

    public void ReportDamageTaken(float damage)
    {
        _tookDamageThisStep = true;
        AddReward(-damage * 0.1f);
    }

    public void ReportShieldAbsorbed(float absorbed)
    {
        AddReward(absorbed * 0.005f);
    }

    public void ReportEnemyKill(float enemyMaxHealth)
    {
        AddReward(enemyMaxHealth * 0.01f);
    }

    public void ReportSelfDeath()
    {
        AddReward(-5f);
        Debug.Log("Reward: " + GetCumulativeReward());
        EndEpisode();
    }

    public void ForceEndEpisode(bool successBonus)
    {
        if (successBonus)
            AddReward(0.5f);
        Debug.Log("Reward: " + GetCumulativeReward());
        EndEpisode();
    }

    private void ApplyShapingRewards()
    {
        AddReward(-_stepPenalty);

        if (_brain == null)
            return;

        CombatSnapshot snapshot = _brain.Snapshot;
        ThreatSnapshot threat = _threatPerception != null
            ? _threatPerception.LastSnapshot
            : default;

        if (snapshot.EnemyCount > 0 && snapshot.NearestEnemyDistance >= 0f)
        {
            float optimal = Mathf.Max(_brain.AttackDistance, 0.5f);
            float delta = Mathf.Abs(snapshot.NearestEnemyDistance - optimal) / optimal;
            AddReward(_distanceRewardScale * (1f - Mathf.Clamp01(delta)));
        }
        else if (PartyManager.Instance?.LeaderTransform != null)
        {
            float dist = Vector2.Distance(transform.position, PartyManager.Instance.LeaderTransform.position);
            if (dist > _maxLeaderFollowDistance)
                AddReward(-_followLeaderPenaltyScale * (dist - _maxLeaderFollowDistance));
        }

        if (threat.NearestEnemyAttacking && !_tookDamageThisStep)
            AddReward(_evadeWhileAttackingReward);

        if (_hadHighThreatLastStep && !_tookDamageThisStep && threat.NearestThreatDistance > 0f
            && threat.NearestThreatDistance <= _nearMissDistance)
        {
            AddReward(_nearMissReward * threat.IncomingThreatUrgency);
        }
        if (_rewardText != null) _rewardText.text = GetCumulativeReward().ToString();
        _hadHighThreatLastStep = threat.IncomingThreatUrgency >= 0.5f;
    }

    private float GetShieldPercent()
    {
        if (_stats == null || _stats.MaxHealth <= 0f)
            return 0f;

        return Mathf.Clamp01(_stats.ShieldTotal / _stats.MaxHealth);
    }

    private float GetProfileObservation()
    {
        if (_brain?.Profile == null)
            return 0f;

        return _brain.Profile.ProfileType switch
        {
            AllyCombatProfileType.Defensive => 0.5f,
            AllyCombatProfileType.Support => 1f,
            _ => 0f
        };
    }

    private float GetWeaponTypeObservation()
    {
        WeaponType type = _brain?.Snapshot.EquippedWeapon ?? WeaponType.Sword;
        return type switch
        {
            WeaponType.Bow => 0.33f,
            WeaponType.Staff => 0.66f,
            WeaponType.Talisman => 1f,
            _ => 0f
        };
    }

    private float GetLeaderDistanceNormalized()
    {
        if (PartyManager.Instance?.LeaderTransform == null)
            return 0f;

        float dist = Vector2.Distance(transform.position, PartyManager.Instance.LeaderTransform.position);
        return NormalizeDistance(dist, 10f);
    }

    private static float NormalizeCount(int count, int max)
    {
        return Mathf.Clamp01(count / (float)Mathf.Max(1, max));
    }

    private static float NormalizeDistance(float distance, float max)
    {
        if (distance < 0f)
            return 1f;
        return Mathf.Clamp01(distance / Mathf.Max(0.01f, max));
    }

    private static float NormalizeSigned(float value)
    {
        return Mathf.Clamp01(value * 0.5f + 0.5f);
    }
}
