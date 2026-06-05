using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// ML-Agents policy for ally positioning: preferred combat distance and retreat urgency.
/// Target/ability selection stays in utility AI.
/// </summary>
[RequireComponent(typeof(AllyAIBrain))]
public class AllyPositionAgent : Agent
{
    [SerializeField] private AllyAIBrain _brain;
    [SerializeField] private AllyMLBridge _mlBridge;

    private CombatPerception _perception;

    public override void Initialize()
    {
        if (_brain == null)
            _brain = GetComponent<AllyAIBrain>();
        if (_mlBridge == null)
            _mlBridge = GetComponent<AllyMLBridge>();
        if (_mlBridge == null)
            _mlBridge = gameObject.AddComponent<AllyMLBridge>();

        _perception = GetComponent<CombatPerception>();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        CombatSnapshot snapshot = _brain != null ? _brain.Snapshot : default;

        sensor.AddObservation(_brain != null ? _brain.HealthPercent : 1f);
        sensor.AddObservation(_brain != null ? _brain.ManaPercent : 1f);
        sensor.AddObservation(NormalizeCount(snapshot.EnemyCount, 8));
        sensor.AddObservation(NormalizeDistance(snapshot.NearestEnemyDistance, 12f));
        sensor.AddObservation(NormalizeCount(snapshot.ClusteredEnemyCount, 6));
        sensor.AddObservation(snapshot.AnyAllyLowHealth ? 1f : 0f);
        sensor.AddObservation(snapshot.AnyEnemyInCombat ? 1f : 0f);
        sensor.AddObservation(GetLeaderDistanceNormalized());
        sensor.AddObservation(_brain != null ? _brain.AttackDistance / 12f : 0.1f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float preferredDistance = Mathf.Clamp01(actions.ContinuousActions[0]);
        float retreatUrgency = Mathf.Clamp01(actions.ContinuousActions[1]);

        _mlBridge.SetMLActions(preferredDistance, retreatUrgency);
        AddReward(-0.001f);

        if (_brain != null && _brain.HealthPercent <= 0f)
            HandleDeathPenalty();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuous = actionsOut.ContinuousActions;
        CombatSnapshot snapshot = _brain != null ? _brain.Snapshot : default;

        float preferred = snapshot.EnemyCount > 0 ? 0.55f : 0.35f;
        float retreat = 0f;

        if (_brain != null)
        {
            if (_brain.HealthPercent < 0.25f)
                retreat = 0.9f;
            else if (_brain.HealthPercent < 0.45f && snapshot.EnemyCount >= 3)
                retreat = 0.6f;
        }

        continuous[0] = preferred;
        continuous[1] = retreat;
    }

    public void ReportDamageDealt(float damage)
    {
        AddReward(damage * 0.01f);
    }

    public void ReportDamageTaken(float damage)
    {
        AddReward(-damage * 0.015f);
    }

    public void ReportAllyDeath()
    {
        AddReward(-1f);
        EndEpisode();
    }

    private void HandleDeathPenalty()
    {
        AddReward(-1f);
        EndEpisode();
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
}
