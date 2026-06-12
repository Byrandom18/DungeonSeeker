using UnityEngine;

/// <summary>
/// Static leader stand-in for training follow-distance rewards.
/// Assign a PlayerStats with _registerAsPrimaryPlayer = true.
/// </summary>
public class AllyTrainingLeaderDummy : MonoBehaviour
{
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private float _patrolRadius = 2f;
    [SerializeField] private float _patrolSpeed = 0.75f;

    private Vector3 _center;
    private float _angle;

    private void Awake()
    {
        if (_stats == null)
            _stats = GetComponent<PlayerStats>();
    }

    private void FixedUpdate()
    {
        if (_patrolRadius <= 0f) return;

        _angle += _patrolSpeed * Time.fixedDeltaTime;
        Vector3 offset = new Vector3(Mathf.Cos(_angle), Mathf.Sin(_angle), 0f) * _patrolRadius;
        transform.position = _center + offset;
    }

    public void ResetToSpawn(Transform spawn)
    {
        _center = spawn != null ? spawn.position : transform.position;
        transform.position = _center;
        _angle = 0f;

        _stats?.ResetForTraining(restoreAlive: true);
    }
}
